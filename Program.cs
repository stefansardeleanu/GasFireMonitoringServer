// File: Program.cs
// Complete Program.cs with enhanced Swagger documentation

using FluentValidation.AspNetCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Filters; // For SwaggerFilters if you created them
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

// Configure Serilog logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{CorrelationId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/gasfiremonitoring-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_485_760, // 10MB
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj} {Properties:j}{NewLine}")
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

// Enhanced Swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gas Fire Monitoring Server API",
        Version = "v1.0",
        Description = @"
            <h2>Professional Gas & Fire Monitoring System API</h2>
            
            <p>This API provides comprehensive monitoring capabilities for industrial gas and fire detection systems across multiple sites.</p>
            
            <h3>🔑 Authentication</h3>
            <p>All endpoints (except login) require JWT authentication. Use <code>/api/auth/login</code> to obtain a token, then include it in the Authorization header:</p>
            <pre>Authorization: Bearer [your-token-here]</pre>
            
            <h3>📊 Key Features</h3>
            <ul>
                <li><strong>Real-time Monitoring:</strong> Live sensor data from industrial sites</li>
                <li><strong>Multi-level Alarms:</strong> Level 1 (Warning) and Level 2 (Critical) alerts</li>
                <li><strong>Geographic Organization:</strong> Sites grouped by counties</li>
                <li><strong>Role-based Access:</strong> CEO, Regional, and Operator roles with site-level permissions</li>
                <li><strong>SignalR Integration:</strong> Real-time updates via WebSocket connection</li>
                <li><strong>SVG Layout Support:</strong> Custom site layouts with sensor positioning</li>
                <li><strong>Dynamic Configuration:</strong> Site and sensor configurations managed through API</li>
            </ul>
            
            <h3>🚨 Sensor Status Codes</h3>
            <ul>
                <li><code>0</code> - Normal Operation</li>
                <li><code>1</code> - Alarm Level 1 (Warning)</li>
                <li><code>2</code> - Alarm Level 2 (Critical)</li>
                <li><code>3</code> - Detector Error</li>
                <li><code>4</code> - Detector Disabled</li>
                <li><code>5</code> - Line Open Fault</li>
                <li><code>6</code> - Line Short Fault</li>
            </ul>
            
            <h3>📡 Real-time Updates</h3>
            <p>Connect to SignalR hub at <code>/monitoringHub</code> for real-time updates. Events include:</p>
            <ul>
                <li><code>SensorUpdate</code> - Sensor value changes</li>
                <li><code>NewAlarm</code> - New alarm triggered</li>
                <li><code>AlarmCleared</code> - Alarm resolved</li>
                <li><code>SiteStatusChanged</code> - Site status update</li>
            </ul>
            
            <h3>📝 API Response Format</h3>
            <p>All endpoints return standardized responses:</p>
            <pre>{
  ""success"": true,
  ""message"": ""Operation completed"",
  ""data"": { ... },
  ""count"": 10,
  ""timestamp"": ""2025-01-15T10:00:00Z""
}</pre>
            
            <h3>⚡ Rate Limiting</h3>
            <p>API requests are limited to 1000 per hour per authenticated user.</p>
            
            <h3>🔒 Security</h3>
            <ul>
                <li>JWT tokens expire after 8 hours</li>
                <li>Passwords hashed with BCrypt (work factor 12)</li>
                <li>Account lockout after 5 failed login attempts</li>
                <li>HTTPS required in production</li>
            </ul>
        ",
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

    // Include XML documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
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

    // Add operation filter for better documentation
    options.OperationFilter<SwaggerDefaultValues>();

    // Uncomment these if you created the SwaggerFilters.cs file:
    options.OperationFilter<SwaggerResponseExampleFilter>();
    options.DocumentFilter<SwaggerDocumentFilter>();

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
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register Infrastructure Services
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<DataProcessingService>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>();

// Enable Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : elapsed > 1000
            ? LogEventLevel.Warning
            : LogEventLevel.Debug;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
        diagnosticContext.Set("RequestContentType", httpContext.Request.ContentType);
        diagnosticContext.Set("RequestContentLength", httpContext.Request.ContentLength ?? 0);
        diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType);
        diagnosticContext.Set("ResponseContentLength", httpContext.Response.ContentLength ?? 0);
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

        // Add custom CSS and JS if you created the files
        // Uncomment these lines if you added the custom CSS and JS files:
        options.InjectStylesheet("/swagger-ui/custom.css");
        options.InjectJavascript("/swagger-ui/custom.js");
    });
}

// Enable CORS
app.UseCors("AllowAllOrigins");

// Serve static files (for Swagger custom CSS/JS if added)
app.UseStaticFiles();

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