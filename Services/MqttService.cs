// File: Services/MqttService.cs
// This implements the IMqttService interface - the actual MQTT connection logic

using System;
using System.Text;
using System.Threading.Tasks;
using System.Buffers; // Needed for ReadOnlySequence<byte>
using MQTTnet;                    // MQTT library
using Microsoft.Extensions.Options;  // For reading configuration
using Microsoft.Extensions.Logging;  // For logging messages
using GasFireMonitoringServer.Configuration;
using GasFireMonitoringServer.Services.Interfaces;

namespace GasFireMonitoringServer.Services
{
    // This class implements the IMqttService interface
    public class MqttService : IMqttService
    {
        // Private fields - internal variables
        private readonly MqttSettings _settings;          // Our configuration
        private readonly ILogger<MqttService> _logger;    // For logging
        private readonly IMqttClient _mqttClient;         // The MQTT client

        // Events - these notify other parts of the program when something happens
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<bool>? ConnectionChanged;

        // Property to check if connected
        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        // Constructor - runs when service is created
        // Parameters are "injected" by the framework (Dependency Injection)
        public MqttService(IOptions<MqttSettings> settings, ILogger<MqttService> logger)
        {
            _settings = settings.Value;  // Extract settings from IOptions wrapper
            _logger = logger;

            // Create MQTT client
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            // Set up event handlers
            SetupEventHandlers();
        }

        // Private method to set up MQTT event handlers
        private void SetupEventHandlers()
        {
            // When connected to broker
            _mqttClient.ConnectedAsync += async (e) =>
            {
                _logger.LogInformation("Connected to MQTT broker");

                // Subscribe to topics
                await _mqttClient.SubscribeAsync(_settings.TopicPattern);

                // Notify listeners
                ConnectionChanged?.Invoke(this, true);
            };

            // When disconnected from broker
            _mqttClient.DisconnectedAsync += async (e) =>
            {
                _logger.LogWarning($"Disconnected from MQTT broker: {e.Reason}");

                // Notify listeners
                ConnectionChanged?.Invoke(this, false);

                // Try to reconnect after 5 seconds
                await Task.Delay(5000);
                await ConnectAsync();
            };

            // When message received
            _mqttClient.ApplicationMessageReceivedAsync += async (e) =>
            {
                // No 'await' needed, so remove 'async' and return Task.CompletedTask
                try
                {
                    // Extract message details
                    var topic = e.ApplicationMessage.Topic;
                    var payload = string.Empty;

                    // Handle payload extraction based on MQTTnet version
                    var sequence = e.ApplicationMessage.Payload;
                    if (!sequence.IsEmpty)
                    {
                        // Convert ReadOnlySequence<byte> to byte[] safely
                        if (sequence.IsSingleSegment)
                        {
                            payload = Encoding.UTF8.GetString(sequence.FirstSpan);
                        }
                        else
                        {
                            var bytes = sequence.ToArray();
                            payload = Encoding.UTF8.GetString(bytes);
                        }
                    }

                    _logger.LogDebug($"Message received - Topic: {topic}");

                    // Notify listeners
                    MessageReceived?.Invoke(this, $"{topic}|{payload}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message");
                }
            };
        }

        // Connect to MQTT broker
        public async Task ConnectAsync()
        {
            try
            {
                // Build connection options
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_settings.BrokerAddress, _settings.Port)
                    .WithCredentials(_settings.Username, _settings.Password)
                    .WithClientId(_settings.ClientId)
                    .WithCleanSession()
                    .Build();

                // Connect
                await _mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT broker");
                throw;
            }
        }

        // Disconnect from broker
        public async Task DisconnectAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }
        }
    }
}