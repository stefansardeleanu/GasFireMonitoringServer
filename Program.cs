// File: Program.cs
// This is the entry point of your application - like the MAIN block in PLC

using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Services;
using GasFireMonitoringServer.Services.Interfaces;
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

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM sensor_last_values";
            var count = await command.ExecuteScalarAsync();
            Console.WriteLine($"✅ Found {count} sensors in database");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    Console.WriteLine($"Full error: {ex}");
}

// Configure the HTTP request pipeline (middleware)
// This is the order in which requests are processed

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();      // Enable Swagger UI
    app.UseSwaggerUI();    // Access at: https://localhost:port/swagger
}

app.UseHttpsRedirection();  // Redirect HTTP to HTTPS
app.UseCors("AllowAllOrigins");  // Enable CORS
app.UseAuthorization();     // Enable authorization
app.MapControllers();       // Map controller endpoints

// Map SignalR hub
app.MapHub<MonitoringHub>("/monitoringHub");

// Start services when application starts
var mqttService = app.Services.GetRequiredService<IMqttService>();
var dataProcessingService = app.Services.GetRequiredService<DataProcessingService>();

// Connect to MQTT broker on startup
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await mqttService.ConnectAsync();
        Console.WriteLine("MQTT Service started and connected");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to start MQTT service: {ex.Message}");
    }
});

// Disconnect from MQTT broker on shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    await mqttService.DisconnectAsync();
    Console.WriteLine("MQTT Service stopped");
});

// Run the application
app.Run();