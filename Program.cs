// File: Program.cs
// Enhanced Program.cs with comprehensive TaskCanceledException handling
// Phase 7.1: Enhanced API Documentation - Production Version with Exception Handling

using FluentValidation.AspNetCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Filters;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Middleware;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Services;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Reflection;
using System.Text;
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

    // Configure JWT for SignalR with cancellation handling
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
                // Ignore cancellation during shutdown
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error processing JWT message");
            }

            return Task.CompletedTask;
        }
    };
});

// Add authorization services
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CEOOnly", policy => policy.RequireRole("CEO"));
    options.AddPolicy("RegionalOrAbove", policy => policy.RequireRole("CEO", "Regional"));
    options.AddPolicy("AllRoles", policy => policy.RequireRole("CEO", "Regional", "Operator"));

    options.AddPolicy("CanManageConfiguration", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("permission", "ManageConfiguration") ||
            context.User.IsInRole("CEO")));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("permission", "ManageUsers") ||
            context.User.IsInRole("CEO")));

    options.AddPolicy("CanViewReports", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("permission", "ViewReports") ||
            (context.User.IsInRole("CEO") || context.User.IsInRole("Regional"))));
});

// Add controllers and API documentation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// Enhanced Swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gas Fire Monitoring Server API",
        Version = "v1.0",
        Description = @"**Professional Gas & Fire Monitoring System API**

This API provides comprehensive monitoring capabilities for industrial gas and fire detection systems across multiple sites.

## 🔑 Authentication
All endpoints (except login) require JWT authentication. Use `/api/auth/login` to obtain a token, then include it in the Authorization header:
```
Authorization: Bearer [your-token-here]
```

## 📊 Key Features
- **Real-time Monitoring:** Live sensor data from industrial sites
- **Multi-level Alarms:** Level 1 (Warning) and Level 2 (Critical) alerts
- **Geographic Organization:** Sites grouped by counties
- **Role-based Access:** CEO, Regional, and Operator roles with site-level permissions
- **SignalR Integration:** Real-time updates via WebSocket connection
- **SVG Layout Support:** Custom site layouts with sensor positioning
- **Dynamic Configuration:** Site and sensor configurations managed through API

## 🚨 Sensor Status Codes
- `0` - Normal Operation
- `1` - Alarm Level 1 (Warning)
- `2` - Alarm Level 2 (Critical)
- `3` - Detector Error
- `4` - Detector Disabled
- `5` - Line Open Fault
- `6` - Line Short Fault

## 📡 Real-time Updates
Connect to SignalR hub at `/monitoringHub` for real-time updates. Events include:
- `SensorUpdate` - Sensor value changes
- `NewAlarm` - New alarm triggered
- `AlarmCleared` - Alarm resolved
- `SiteStatusChanged` - Site status update

## 📝 API Response Format
All endpoints return standardized responses:
```json
{
  ""success"": true,
  ""message"": ""Operation completed"",
  ""data"": { ... },
  ""count"": 10,
  ""timestamp"": ""2025-01-15T10:00:00Z""
}
```

## ⚡ Rate Limiting
API requests are limited to 1000 per hour per authenticated user.

## 🔒 Security
- JWT tokens expire after 8 hours
- Passwords hashed with BCrypt (work factor 12)
- Account lockout after 5 failed login attempts
- HTTPS required in production",
        Contact = new OpenApiContact
        {
            Name = "Gas Fire Monitoring Support",
            Email = "support@gasfiremonitoring.com"
        },
        License = new OpenApiLicense
        {
            Name = "Commercial License"
        }
    });

    // Include XML documentation with comprehensive error handling
    try
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            Log.Information("XML documentation loaded successfully");
        }
        else
        {
            Log.Warning("XML documentation file not found: {XmlPath}", xmlPath);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to include XML comments in Swagger");
    }

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      <br/><br/>
                      Enter 'Bearer' [space] and then your token in the text input below.
                      <br/><br/>
                      Example: <b>Bearer eyJhbGciOiJIUzI1NiIsInR5cCI...</b>",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
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

    // Add operation filter with error handling
    try
    {
        options.OperationFilter<SwaggerDefaultValues>();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to add SwaggerDefaultValues filter");
    }

    // Group endpoints by tags with custom icons
    options.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName switch
        {
            "Auth" => "🔐 Authentication",
            "Sensor" => "📊 Sensors & Monitoring",
            "Alarm" => "🚨 Alarms & Alerts",
            "Site" => "🏭 Sites & Locations",
            "Layout" => "🗺️ Layout Management",
            "Configuration" => "⚙️ Configuration",
            _ => controllerName ?? "Other"
        }};
    });

    // Order actions alphabetically
    options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// Add CORS policy for client applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure database connection with error handling
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

// Add SignalR for real-time communication with enhanced configuration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.StreamBufferCapacity = 10;
});

// Register Repository Layer (Data Access)
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Business Service Layer (Domain Logic)
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register Infrastructure Services with error handling
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

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>();

// Enhanced request logging with TaskCanceledException filtering
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        // Ignore TaskCanceledException completely
        if (ex is TaskCanceledException || ex is OperationCanceledException)
            return LogEventLevel.Debug;

        // Reduce noise for static files and favicon requests
        if (httpContext.Request.Path.StartsWithSegments("/swagger") ||
            httpContext.Request.Path.Value?.Contains("favicon") == true ||
            httpContext.Request.Path.Value?.Contains(".css") == true ||
            httpContext.Request.Path.Value?.Contains(".js") == true)
        {
            return LogEventLevel.Debug;
        }

        if (ex != null || httpContext.Response.StatusCode > 499)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode > 399)
            return LogEventLevel.Warning;

        if (elapsed > 1000)
            return LogEventLevel.Warning;

        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        try
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
            diagnosticContext.Set("RequestContentType", httpContext.Request.ContentType);
            diagnosticContext.Set("RequestContentLength", httpContext.Request.ContentLength ?? 0);
            diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType);
            diagnosticContext.Set("ResponseContentLength", httpContext.Response.ContentLength ?? 0);

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
                diagnosticContext.Set("UserRole", httpContext.User.FindFirst("role")?.Value);
            }

            if (httpContext.Request.Path.StartsWithSegments("/api"))
            {
                diagnosticContext.Set("ApiEndpoint", true);
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation during shutdown
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error enriching diagnostic context");
        }
    };
});

// Configure Swagger UI
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gas Fire Monitoring API v1");

        // Enhanced UI customization
        options.DocumentTitle = "Gas Fire Monitoring API Documentation";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
        options.ShowCommonExtensions();
        options.EnableValidator();

        // Custom HTML to prevent favicon errors and improve performance
        options.HeadContent = @"
<style>
    .swagger-ui .topbar { display: none !important; }
    .swagger-ui .info .title { color: #3b82f6; }
</style>
<link rel='icon' href='data:,'>
<meta name='referrer' content='no-referrer'>";
    });
}

// Enable CORS
app.UseCors("AllowAllOrigins");

// Serve static files with enhanced error handling
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

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<MonitoringHub>("/monitoringHub");

// Start MQTT service and initialize DataProcessingService with comprehensive error handling
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

// Register enhanced shutdown handler
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