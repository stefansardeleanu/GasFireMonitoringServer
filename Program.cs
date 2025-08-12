// File: Program.cs
// Complete Program.cs without API versioning and rate limiting (keep it simple)

using FluentValidation.AspNetCore;
using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Infrastructure;
using GasFireMonitoringServer.Middleware;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Repositories;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services;
using GasFireMonitoringServer.Services.Business;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Services.Infrastructure;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text;

// Enhanced Serilog configuration with structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("MQTTnet", LogEventLevel.Information)

    // Enrich with structured data
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithProperty("Application", "GasFireMonitoringServer")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.WithProperty("ProcessId", Environment.ProcessId)

    // Console output with enhanced template
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj} " +
        "{Properties:j}{NewLine}{Exception}")

    // File output with detailed logging
    .WriteTo.File(
        path: "logs/gasfiremonitoring-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50_000_000,
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] " +
            "{SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")

    // Separate file for errors only
    .WriteTo.File(
        path: "logs/errors-.log",
        restrictedToMinimumLevel: LogEventLevel.Warning,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] " +
            "{SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")

    // Performance logs
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("Performance"))
        .WriteTo.File(
            path: "logs/performance-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj} {Properties:j}{NewLine}"))

    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Configure JWT settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettingsDto>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT settings not found in configuration");
}

builder.Services.Configure<JwtSettingsDto>(builder.Configuration.GetSection("JwtSettings"));

// Configure MQTT settings from appsettings.json
builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));

// Add JWT authentication
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

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/monitoringHub"))
            {
                context.Token = accessToken;
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gas Fire Monitoring Server API",
        Version = "v1.0",
        Description = @"
            **Professional API for gas and fire monitoring system**
            
            This API provides comprehensive monitoring capabilities for industrial gas and fire detection systems across multiple sites.
            
            ### Features:
            - **Real-time sensor monitoring** across 10+ industrial sites
            - **JWT authentication** with role-based access control
            - **Configuration management** for sites, sensors, and layouts
            - **SVG layout management** with sensor positioning
            - **SignalR real-time updates** for live monitoring
            - **MQTT integration** for PLC data collection
            
            ### Authentication:
            All endpoints require JWT authentication. Use the `/api/auth/login` endpoint to obtain a token.
        ",
        Contact = new OpenApiContact
        {
            Name = "Gas Fire Monitoring System",
            Email = "support@gasfiremonitoring.com"
        }
    });

    // Include XML documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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

    options.OperationFilter<SwaggerDefaultValues>();

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

    options.OrderActionsBy(apiDesc => apiDesc.GroupName);
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

// Configure database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Register Repository Layer (Data Access)
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Business Service Layer (Domain Logic)  
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register Infrastructure Service Layer
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddScoped<DataProcessingService>();
builder.Services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gas Fire Monitoring Server API v1.0");
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
        c.DisplayOperationId();
    });
}

// Enable CORS
app.UseCors("AllowAllOrigins");

// Enable serving static files (for SVG layouts)
app.UseStaticFiles();

// Performance and request logging middleware
app.UseMiddleware<PerformanceLoggingMiddleware>();

// Enhanced Serilog request logging
app.UseSerilogRequestLogging(configure =>
{
    configure.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms [{CorrelationId}]";
    configure.IncludeQueryInRequestPath = true;
    configure.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode > 499)
            return LogEventLevel.Error;
        if (elapsed > 1000 || httpContext.Response.StatusCode > 399)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
    configure.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestSize", httpContext.Request.ContentLength ?? 0);
        diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength ?? 0);

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
            diagnosticContext.Set("UserRole", httpContext.User.FindFirst("role")?.Value);
        }

        if (httpContext.Request.Path.StartsWithSegments("/api"))
        {
            diagnosticContext.Set("ApiEndpoint", true);
        }
    };
});

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<MonitoringHub>("/monitoringHub");

// Start MQTT service
try
{
    Log.Information("Starting MQTT service...");
    var mqttService = app.Services.GetRequiredService<IMqttService>();
    await mqttService.ConnectAsync();
    Log.Information("MQTT service started successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to start MQTT service");
    throw;
}

// Register shutdown handler
app.Lifetime.ApplicationStopping.Register(async () =>
{
    Log.Information("Application stopping, disconnecting from MQTT...");
    try
    {
        var mqttService = app.Services.GetService<IMqttService>();
        if (mqttService != null)
        {
            await mqttService.DisconnectAsync();
            Log.Information("Disconnected from MQTT broker");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error disconnecting from MQTT broker");
    }
});

Log.Information("Gas Fire Monitoring Server starting up...");

app.Run();

Log.CloseAndFlush();