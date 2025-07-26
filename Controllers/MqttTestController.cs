// File: Controllers/MqttTestController.cs
// Test controller for MQTT functionality

using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using MQTTnet;
using GasFireMonitoringServer.Configuration;
using Microsoft.Extensions.Options;

namespace GasFireMonitoringServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MqttTestController : ControllerBase
    {
        private readonly ILogger<MqttTestController> _logger;
        private readonly MqttSettings _mqttSettings;

        public MqttTestController(ILogger<MqttTestController> logger, IOptions<MqttSettings> mqttSettings)
        {
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishTestMessage()
        {
            try
            {
                // Create a test MQTT client to publish a message
                var factory = new MqttClientFactory();
                using var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_mqttSettings.BrokerAddress, _mqttSettings.Port)
                    .WithCredentials(_mqttSettings.Username, _mqttSettings.Password)
                    .WithClientId($"{_mqttSettings.ClientId}-test")
                    .Build();

                await mqttClient.ConnectAsync(options);

                // Create test payload
                var payload = new
                {
                    rCH41_mA = "5.000000E+00",
                    rCH41_PV = "1.250000",
                    iCH41_DetStatus = "0",
                    strCH41_TAG = "TEST-41",
                    iCH41_DetType = "1"
                };

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("/PLCNEXT/5_PanouHurezani/CH41")
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithRetainFlag(false)
                    .Build();

                await mqttClient.PublishAsync(message);
                await mqttClient.DisconnectAsync();

                _logger.LogInformation("Test MQTT message published");

                return Ok(new
                {
                    success = true,
                    message = "Test message published",
                    topic = "/PLCNEXT/5_PanouHurezani/CH41",
                    payload = payload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish test message");
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost("publish-alarm")]
        public async Task<IActionResult> PublishTestAlarm()
        {
            try
            {
                // Create a test MQTT client to publish an alarm
                var factory = new MqttClientFactory();
                using var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_mqttSettings.BrokerAddress, _mqttSettings.Port)
                    .WithCredentials(_mqttSettings.Username, _mqttSettings.Password)
                    .WithClientId($"{_mqttSettings.ClientId}-test-alarm")
                    .Build();

                await mqttClient.ConnectAsync(options);

                // Create test alarm message
                var alarmMessage = $"DT#{DateTime.Now:yyyy-MM-dd-HH:mm:ss.ff}, Alarm Level 2, TEST-41";

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("/PLCNEXT/5_PanouHurezani/alarms")
                    .WithPayload(alarmMessage)
                    .WithRetainFlag(false)
                    .Build();

                await mqttClient.PublishAsync(message);
                await mqttClient.DisconnectAsync();

                _logger.LogInformation($"Test alarm message published: {alarmMessage}");

                return Ok(new
                {
                    success = true,
                    message = "Test alarm published",
                    topic = "/PLCNEXT/5_PanouHurezani/alarms",
                    payload = alarmMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish test alarm");
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("settings")]
        public IActionResult GetMqttSettings()
        {
            return Ok(new
            {
                broker = _mqttSettings.BrokerAddress,
                port = _mqttSettings.Port,
                clientId = _mqttSettings.ClientId,
                topicPattern = _mqttSettings.TopicPattern,
                hasUsername = !string.IsNullOrEmpty(_mqttSettings.Username)
            });
        }
    }
}