// File: Controllers/HealthController.cs
// Health check and diagnostic endpoints

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GasFireMonitoringServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            ApplicationDbContext context,
            IServiceProvider serviceProvider,
            ILogger<HealthController> logger)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                var sensorCount = await _context.Sensors.CountAsync();

                // Get MQTT service status
                var mqttService = _serviceProvider.GetService<IMqttService>();
                var mqttConnected = mqttService?.IsConnected ?? false;

                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    database = new
                    {
                        connected = canConnect,
                        sensorCount = sensorCount
                    },
                    mqtt = new
                    {
                        connected = mqttConnected
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Ok(new
                {
                    status = "unhealthy",
                    error = ex.Message
                });
            }
        }

        [HttpPost("test-sensor")]
        public async Task<IActionResult> CreateTestSensor()
        {
            try
            {
                var testSensor = new Sensor
                {
                    SiteId = 5,
                    SiteName = "PanouHurezani",
                    ChannelId = "99",
                    TagName = "TEST-99",
                    DetectorType = 1,
                    ProcessValue = 1.25,
                    CurrentValue = 5.0,
                    Status = 0,
                    StatusText = "Normal",
                    Units = "%LEL",
                    LastUpdated = DateTime.UtcNow,
                    Topic = "/PLCNEXT/5_PanouHurezani/CH99",
                    RawJson = "{\"test\": true}"
                };

                _context.Sensors.Add(testSensor);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Test sensor created with ID: {testSensor.Id}");

                return Ok(new
                {
                    success = true,
                    message = "Test sensor created successfully",
                    sensorId = testSensor.Id,
                    sensor = testSensor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create test sensor");
                return Ok(new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("mqtt-status")]
        public IActionResult GetMqttStatus()
        {
            try
            {
                var mqttService = _serviceProvider.GetService<IMqttService>();

                return Ok(new
                {
                    connected = mqttService?.IsConnected ?? false,
                    message = mqttService == null ? "MQTT service not found" : "MQTT service found"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message
                });
            }
        }
    }
}