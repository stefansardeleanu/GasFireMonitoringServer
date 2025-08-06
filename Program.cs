// File: Program.cs
// This is the entry point of your application - like the MAIN block in PLC

using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Services;
using GasFireMonitoringServer.Services.Interfaces;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (Dependency Injection setup)
// This is like declaring which function blocks are available in your PLC

// Add controllers for REST API
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Configure CORS (Cross-Origin Resource Sharing) - allows clients to connect
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add database context - connection to MariaDB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var serverVersion = ServerVersion.Create(10, 5, 0, ServerType.MariaDb);
    options.UseMySql(connectionString, serverVersion);
});

// Configure settings from appsettings.json
builder.Services.Configure<MqttSettings>(
    builder.Configuration.GetSection("MqttSettings"));

// Register Repository Layer (Data Access)
// Scoped = one instance per HTTP request
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();

// Register services (like declaring instances of function blocks)
// Singleton = only one instance for entire application lifetime
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
    app.UseSwaggerUI();
}

// Add middleware in order
app.UseCors("AllowAllOrigins");  // Enable CORS
app.UseAuthorization();         // Add authorization
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

// Run the application
app.Run();