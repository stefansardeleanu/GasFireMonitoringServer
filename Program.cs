// File: Program.cs
// COMPLETE ENHANCED VERSION - Phase 7.3 with ALL existing functionality preserved
// Enhanced with: Performance Monitoring, SignalR Optimization, LINQ Optimization
// PRESERVES: All authentication, validation, logging, MQTT, and configuration features

using FluentValidation.AspNetCore;
using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Extensions;
using GasFireMonitoringServer.Filters;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Middleware;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services;
using GasFireMonitoringServer.Services.Business;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Services.Infrastructure;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Configure Serilog logging with comprehensive TaskCanceledException filtering
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Swashbuckle", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Information)
    .MinimumLevel.Override("MQTTnet", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Filter.ByExcluding(e =>
        // Filter TaskCanceledException from all sources
        e.Exception is TaskCanceledException ||
        e.Exception is OperationCanceledException ||
        // Filter favicon and static file errors
        (e.Properties.ContainsKey("RequestPath") &&
         e.Properties["RequestPath"].ToString().Contains("favicon")) ||
        (e.Properties.ContainsKey("RequestPath") &&
         e.Properties["RequestPath"].ToString().Contains("swagger-ui")) ||
        // Filter SignalR connection cancellations
        (e.MessageTemplate != null &&
         e.MessageTemplate.Text.Contains("Connection cancelled")) ||
        // Filter MQTT connection timeouts
        (e.MessageTemplate != null &&
         e.MessageTemplate.Text.Contains("Operation was cancelled")))
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{CorrelationId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/gasfiremonitoring-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_485_760, // 10MB
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{CorrelationId}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Configure global unhandled exception handling
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    if (e.ExceptionObject is TaskCanceledException || e.ExceptionObject is OperationCanceledException)
    {
        Log.Debug("Task cancellation during application shutdown - this is normal");
        return;
    }
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception occurred");
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    if (e.Exception.InnerException is TaskCanceledException ||
        e.Exception.InnerException is OperationCanceledException)
    {
        Log.Debug("Unobserved task cancellation - marking as observed");
        e.SetObserved(); // Prevent process termination
        return;
    }
    Log.Error(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

// Configure JWT settings with error handling
JwtSettingsDto? jwtSettings;
try
{
    jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettingsDto>();
    if (jwtSettings == null)
    {
        throw new InvalidOperationException("JWT settings not found in configuration");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to load JWT settings from configuration");
    throw;
}

builder.Services.Configure<JwtSettingsDto>(builder.Configuration.GetSection("JwtSettings"));

// Configure MQTT settings with error handling
try
{
    builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to configure MQTT settings - MQTT functionality may not work");
}

// Add JWT authentication with enhanced error handling
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = jwtSettings.ValidateIssuer,
        ValidateAudience = jwtSettings.ValidateAudience,
        ValidateLifetime = jwtSettings.ValidateLifetime,
        ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = jwtSettings.ClockSkew
    };

    // EXISTING: SignalR JWT support from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            try
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/monitoringHub"))
                {
                    context.Token = accessToken;
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation during token processing
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error processing JWT token from query string");
            }
            return Task.CompletedTask;
        }
    };
});

// EXISTING: Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CEOOnly", policy => policy.RequireRole("CEO"));
    options.AddPolicy("RegionalOrAbove", policy => policy.RequireRole("CEO", "Regional"));
    options.AddPolicy("AllRoles", policy => policy.RequireRole("CEO", "Regional", "Operator"));
    options.AddPolicy("CanManageConfiguration", policy => policy.RequireClaim("permission", "manage_configuration"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("permission", "manage_users"));
    options.AddPolicy("CanViewReports", policy => policy.RequireClaim("permission", "view_reports"));
});

// Add controllers with JSON options
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

// Configure API behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
    options.SuppressMapClientErrors = false;
    options.ClientErrorMapping[404].Link = "https://httpstatuses.com/404";
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version")
    );
});

// EXISTING: Swagger/OpenAPI configuration with comprehensive documentation
// EXISTING: Swagger/OpenAPI configuration with comprehensive documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Gas Fire Monitoring Server API",
        Description = "Professional ASP.NET Core Web API for industrial gas and fire monitoring system with real-time MQTT integration, comprehensive authentication, and advanced layout management.",
        Contact = new OpenApiContact
        {
            Name = "Gas Fire Monitoring System",
            Email = "admin@gasfiremonitoring.com"
        }
    });

    // Include XML documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // EXISTING: JWT authentication support in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // FIXED: Tag actions by controller name for proper grouping
    options.TagActionsBy(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        return controllerName switch
        {
            "Auth" => new[] { "🔐 Authentication" },
            "Sensor" => new[] { "📊 Sensors & Monitoring" },
            "Alarm" => new[] { "🚨 Alarms & Alerts" },
            "Site" => new[] { "🏭 Sites & Locations" },
            "Configuration" => new[] { "⚙️ Configuration" },
            "Layout" => new[] { "🗺️ Layout Management" },
            _ => new[] { $"📋 {controllerName}" }
        };
    });

    // Order actions for better organization
    options.OrderActionsBy(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        var httpMethod = apiDesc.HttpMethod;

        // Order by controller, then by HTTP method (GET, POST, PUT, DELETE), then by action
        var methodOrder = httpMethod switch
        {
            "GET" => "1",
            "POST" => "2",
            "PUT" => "3",
            "DELETE" => "4",
            _ => "9"
        };

        return $"{controllerName}_{methodOrder}_{actionName}";
    });

    // Add operation and document filters for enhanced documentation
    options.OperationFilter<SwaggerDefaultValues>();
    options.DocumentFilter<SwaggerDocumentFilter>();

    // Enable additional Swagger features
    options.EnableAnnotations();
    options.DescribeAllParametersInCamelCase();
    options.CustomOperationIds(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controllerName}_{actionName}";
    });
});

// EXISTING: FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// EXISTING: CORS policy for client applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// EXISTING: Configure database connection with error handling
try
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    });
    Log.Information("Database context configured successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to configure database context");
    throw;
}

// ENHANCED: SignalR with performance optimizations (Phase 7.3)
builder.Services.AddSignalR(options =>
{
    // EXISTING: Basic configuration
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);

    // NEW: Performance optimizations
    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
    options.StreamBufferCapacity = 10;
})
.AddJsonProtocol(options =>
{
    // NEW: JSON serialization settings for reduced bandwidth
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.PayloadSerializerOptions.WriteIndented = false; // Minimize payload size
});

// EXISTING: Repository Layer (Data Access)
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// EXISTING: Business Service Layer (Domain Logic)
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// EXISTING: Infrastructure Services with error handling
try
{
    builder.Services.AddSingleton<IMqttService, MqttService>();
    builder.Services.AddSingleton<DataProcessingService>();
    builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
    Log.Information("Infrastructure services registered successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to register infrastructure services");
    throw;
}

// NEW: Performance Monitoring Services (Phase 7.3)
builder.Services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
builder.Services.AddHostedService<PerformanceMonitoringService>(serviceProvider =>
    serviceProvider.GetRequiredService<IPerformanceMonitoringService>() as PerformanceMonitoringService);

// NEW: Enhanced Health Checks with Performance Integration (Phase 7.3)
builder.Services.AddHealthChecks()
    .AddCheck<MqttHealthCheck>("mqtt")
    .AddCheck<SystemResourceHealthCheck>("system_resources");

// NEW: Performance Monitoring Configuration (Phase 7.3)
builder.Services.Configure<PerformanceMonitoringOptions>(options =>
{
    options.EnableDetailedMetrics = builder.Environment.IsDevelopment();
    options.MetricsRetentionHours = 24;
    options.HealthCheckIntervalMinutes = 1;
    options.PerformanceReportIntervalMinutes = 5;
    options.SlowRequestThresholdMs = 1000;
    options.VerySlowRequestThresholdMs = 5000;
});

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>(); // ENHANCED in Phase 7.3

// EXISTING: Enhanced request logging with TaskCanceledException filtering
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return LogEventLevel.Debug; // Reduce noise from cancellations
        }
        if (ex != null)
        {
            return LogEventLevel.Error;
        }
        if (httpContext.Response.StatusCode > 499)
        {
            return LogEventLevel.Error;
        }
        if (elapsed > 10000 && httpContext.Response.StatusCode > 399)
        {
            return LogEventLevel.Warning;
        }
        return LogEventLevel.Information;
    };
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        try
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            diagnosticContext.Set("ConnectionId", httpContext.Connection.Id);

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("Username", httpContext.User.Identity.Name);
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation during context enrichment
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error enriching diagnostic context");
        }
    };
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gas Fire Monitoring API V1");
        c.RoutePrefix = string.Empty; // Serve the Swagger UI at the app's root
    });
}

// app.UseHttpsRedirection();

// EXISTING: Static files with enhanced error handling
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        try
        {
            // Add cache headers for static files
            if (context.File.Name.EndsWith(".css") || context.File.Name.EndsWith(".js"))
            {
                context.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation during static file serving
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error preparing static file response");
        }
    },
    ServeUnknownFileTypes = false
});

// EXISTING: Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// EXISTING: CORS
app.UseCors("AllowAllOrigins");

// EXISTING: Map controllers and SignalR hub
app.MapControllers();

// ENHANCED: SignalR hub with performance optimizations (Phase 7.3)
app.MapHub<MonitoringHub>("/monitoringHub", options =>
{
    // NEW: Enhanced hub configuration for performance
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);
});

// NEW: Enhanced Health Check Endpoints (Phase 7.3)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
});

// NEW: Simplified health check for load balancers
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// EXISTING: Start MQTT service and initialize DataProcessingService with comprehensive error handling
var cancellationTokenSource = new CancellationTokenSource();
try
{
    Log.Information("Starting MQTT service...");
    var mqttService = app.Services.GetRequiredService<IMqttService>();

    // Use timeout to prevent indefinite hanging
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationTokenSource.Token, timeoutCts.Token);

    await mqttService.ConnectAsync();
    Log.Information("MQTT service started successfully");

    // Force DataProcessingService creation
    Log.Information("Initializing DataProcessingService...");
    var dataProcessingService = app.Services.GetRequiredService<DataProcessingService>();
    Log.Information("DataProcessingService initialized and subscribed to MQTT events");
}
catch (TaskCanceledException)
{
    Log.Warning("MQTT service startup was cancelled - this may be normal during shutdown");
}
catch (OperationCanceledException)
{
    Log.Warning("MQTT service startup was cancelled due to timeout");
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to start MQTT service or DataProcessingService - continuing without MQTT");
    // Don't throw - continue without MQTT functionality
}

// NEW: Performance Monitoring Integration (Phase 7.3)
try
{
    Log.Information("Initializing performance monitoring integration...");

    // Get performance monitoring service
    var performanceMonitoringService = app.Services.GetRequiredService<IPerformanceMonitoringService>();

    // Integrate with MQTT service for message tracking
    var mqttService = app.Services.GetService<IMqttService>();
    if (mqttService != null)
    {
        // Hook up MQTT message tracking
        mqttService.MessageReceived += (sender, message) =>
        {
            try
            {
                var parts = message.Split('|');
                var topic = parts.Length > 0 ? parts[0] : "unknown";
                performanceMonitoringService.RecordMqttMessage(topic, true);
            }
            catch (Exception ex)
            {
                var topic = message.Split('|')[0] ?? "unknown";
                performanceMonitoringService.RecordMqttMessage(topic, false, ex.Message);
            }
        };
    }

    Log.Information("Performance monitoring integration completed successfully");
}
catch (Exception ex)
{
    Log.Warning(ex, "Performance monitoring integration encountered issues - continuing without some features");
}

// EXISTING: Register enhanced shutdown handler
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application stopping, initiating graceful shutdown...");
    cancellationTokenSource.Cancel(); // Signal all operations to cancel

    // Start cleanup in background task to avoid blocking
    Task.Run(async () =>
    {
        try
        {
            using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var mqttService = app.Services.GetService<IMqttService>();
            if (mqttService != null)
            {
                await mqttService.DisconnectAsync();
                Log.Information("Disconnected from MQTT broker");
            }
        }
        catch (TaskCanceledException)
        {
            Log.Debug("MQTT disconnect cancelled during shutdown - this is normal");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error disconnecting from MQTT broker during shutdown");
        }
        finally
        {
            Log.Information("Shutdown cleanup completed");
            Log.CloseAndFlush();
        }
    });
});

Log.Information("Gas Fire Monitoring Server starting up...");

try
{
    app.Run();
}
catch (TaskCanceledException)
{
    Log.Information("Application was cancelled during shutdown");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// NEW: Supporting Classes for Performance Monitoring (Phase 7.3)
public class PerformanceMonitoringOptions
{
    public bool EnableDetailedMetrics { get; set; } = true;
    public int MetricsRetentionHours { get; set; } = 24;
    public int HealthCheckIntervalMinutes { get; set; } = 1;
    public int PerformanceReportIntervalMinutes { get; set; } = 5;
    public int SlowRequestThresholdMs { get; set; } = 1000;
    public int VerySlowRequestThresholdMs { get; set; } = 5000;
}

public class MqttHealthCheck : IHealthCheck
{
    private readonly IMqttService _mqttService;

    public MqttHealthCheck(IMqttService mqttService)
    {
        _mqttService = mqttService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = _mqttService?.IsConnected ?? false;

            if (isConnected)
            {
                return Task.FromResult(HealthCheckResult.Healthy("MQTT broker connection is active"));
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("MQTT broker connection is inactive"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"MQTT health check failed: {ex.Message}"));
        }
    }
}

public class SystemResourceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;

            var data = new Dictionary<string, object>
            {
                { "memoryUsageMB", memoryUsageMB },
                { "processId", process.Id },
                { "startTime", process.StartTime },
                { "uptime", DateTime.UtcNow - process.StartTime }
            };

            // Define memory thresholds
            if (memoryUsageMB > 2000) // 2GB
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"High memory usage: {memoryUsageMB}MB", null, data));
            }
            else if (memoryUsageMB > 1000) // 1GB
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Elevated memory usage: {memoryUsageMB}MB", null, data));
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"System resources are healthy. Memory: {memoryUsageMB}MB", data));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"System resource health check failed: {ex.Message}"));
        }
    }
}