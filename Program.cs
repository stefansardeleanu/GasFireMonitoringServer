// File: Program.cs
// Main application entry point
// UPDATED: Added JWT authentication configuration

using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
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
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure JWT settings from appsettings.json
builder.Services.Configure<JwtSettingsDto>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettingsDto>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT settings not found in configuration");
}

// Validate JWT settings
var validationErrors = jwtSettings.Validate();
if (validationErrors.Any())
{
    throw new InvalidOperationException($"Invalid JWT settings: {string.Join(", ", validationErrors)}");
}

// Add authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
    options.SaveToken = true;
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
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Gas Fire Monitoring Server API",
        Version = "v1",
        Description = "Professional API for gas and fire monitoring system with JWT authentication"
    });

    // Configure Swagger to use JWT
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
builder.Services.AddScoped<IUserRepository, UserRepository>(); // NEW: User repository

// Register Business Service Layer (Domain Logic)  
// Scoped = one instance per HTTP request
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>(); // NEW: Authentication service

// Register Infrastructure Services (Singleton = one instance for entire application lifetime)
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<DataProcessingService>();

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();  // Log to console
    config.AddDebug();    // Log to debug output
});

// Build the application
var app = builder.Build();

// Test database connection
try
{
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Testing connection to: {connectionString}");

    using (var connection = new MySqlConnector.MySqlConnection(connectionString))
    {
        await connection.OpenAsync();
        Console.WriteLine("✅ Direct MySQL connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gas Fire Monitoring API v1");
        options.RoutePrefix = "swagger";

        // Add instructions for JWT authentication
        options.DocumentTitle = "Gas Fire Monitoring API - Authentication Required";
    });
}

// Add middleware in the correct order (IMPORTANT: Order matters!)
app.UseCors("AllowAllOrigins");  // Enable CORS

// Authentication and Authorization middleware (NEW)
app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();   // Must come after UseAuthentication

app.MapControllers();           // Map API controllers
app.MapHub<MonitoringHub>("/monitoringHub");  // Map SignalR hub

// Start services when application starts
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        var mqttService = app.Services.GetRequiredService<IMqttService>();
        await mqttService.ConnectAsync();
        Console.WriteLine("✅ MQTT Service connected successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ MQTT service failed to connect: {ex.Message}");
    }
});

// Disconnect from MQTT broker on shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        var mqttService = app.Services.GetRequiredService<IMqttService>();
        await mqttService.DisconnectAsync();
        Console.WriteLine("MQTT Service disconnected");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error disconnecting MQTT service: {ex.Message}");
    }
});

Console.WriteLine("🚀 Gas Fire Monitoring Server is running...");
Console.WriteLine("📊 Swagger UI available at: http://localhost:5208/swagger");
Console.WriteLine("🔌 SignalR Hub available at: http://localhost:5208/monitoringHub");
Console.WriteLine("🔐 JWT Authentication enabled - Login required for protected endpoints");

// Run the application
app.Run();