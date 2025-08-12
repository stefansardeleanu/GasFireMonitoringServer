// File: Program.cs
// Complete Program.cs with enhanced logging, authentication, and all services

using FluentValidation.AspNetCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
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
using Serilog;
using Serilog.Events;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithProperty("Application", "GasFireMonitoringServer")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/gasfiremonitoring-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
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

            // Allow JWT token from query string for SignalR connections
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
    // Define authorization policies
    options.AddPolicy("CEOOnly", policy => policy.RequireRole("CEO"));
    options.AddPolicy("RegionalOrAbove", policy => policy.RequireRole("CEO", "Regional"));
    options.AddPolicy("AllRoles", policy => policy.RequireRole("CEO", "Regional", "Operator"));

    // Custom policies for permissions
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
        Version = "v1",
        Description = "Professional API for gas and fire monitoring system with JWT authentication"
    });

    // Configure Swagger to use JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Register Repository Layer (Data Access)
// Scoped = one instance per HTTP request
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Business Service Layer (Domain Logic)  
// Scoped = one instance per HTTP request
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register Infrastructure Service Layer (Infrastructure Logic)
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddScoped<DataProcessingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gas Fire Monitoring Server API v1");
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}

// Enable CORS
app.UseCors("AllowAllOrigins");

// Enable serving static files (for SVG layouts)
app.UseStaticFiles();

// Add Serilog request logging
app.UseSerilogRequestLogging(configure =>
{
    configure.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    configure.IncludeQueryInRequestPath = true;
    configure.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
            diagnosticContext.Set("UserRole", httpContext.User.FindFirst("role")?.Value);
        }
    };
});

// Add custom middleware (order is important!)
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<MonitoringHub>("/monitoringHub");

// Start MQTT service
var mqttService = app.Services.GetRequiredService<IMqttService>();
try
{
    await mqttService.ConnectAsync();
    Log.Information("MQTT service started successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to start MQTT service");
}

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        Log.Information("Application is shutting down, disconnecting MQTT service");
        await mqttService.DisconnectAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during MQTT service shutdown");
    }
    finally
    {
        Log.CloseAndFlush();
    }
});

Log.Information("Gas Fire Monitoring Server starting up...");
app.Run();