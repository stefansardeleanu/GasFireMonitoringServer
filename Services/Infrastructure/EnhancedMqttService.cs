// File: Services/Infrastructure/EnhancedMqttService.cs
// PHASE 7.3 TASK 3: Enhanced MQTT service with performance monitoring
// Wrapper for existing MQTT service to add performance tracking

using System;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;

namespace GasFireMonitoringServer.Services.Infrastructure
{
    /// <summary>
    /// Enhanced MQTT service wrapper with performance monitoring
    /// Integrates with existing MqttService to add performance tracking
    /// </summary>
    public class EnhancedMqttService
    {
        private readonly IMqttService _mqttService;
        private readonly IPerformanceMonitoringService _performanceService;
        private readonly ILogger<EnhancedMqttService> _logger;

        public EnhancedMqttService(
            IMqttService mqttService,
            IPerformanceMonitoringService performanceService,
            ILogger<EnhancedMqttService> logger)
        {
            _mqttService = mqttService;
            _performanceService = performanceService;
            _logger = logger;

            // Hook into existing MQTT events
            HookIntoMqttEvents();
        }

        /// <summary>
        /// Hook into existing MQTT service events for performance tracking
        /// </summary>
        private void HookIntoMqttEvents()
        {
            if (_mqttService != null)
            {
                // Track message processing performance
                _mqttService.MessageReceived += OnMqttMessageReceived;
                _mqttService.ConnectionChanged += OnMqttConnectionChanged;
            }
        }

        /// <summary>
        /// Handle MQTT message received with performance tracking
        /// </summary>
        private void OnMqttMessageReceived(object sender, string message)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Extract topic from message
                var parts = message.Split('|');
                var topic = parts.Length > 0 ? parts[0] : "unknown";

                // Record successful message processing
                _performanceService.RecordMqttMessage(topic, true);

                stopwatch.Stop();

                // Log slow message processing
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    _logger.LogWarning("Slow MQTT message processing: Topic {Topic} took {ElapsedMs}ms",
                        topic, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var topic = "unknown";
                try
                {
                    var parts = message.Split('|');
                    topic = parts.Length > 0 ? parts[0] : "unknown";
                }
                catch { }

                _performanceService.RecordMqttMessage(topic, false, ex.Message);
                _logger.LogError(ex, "Error processing MQTT message for topic {Topic}", topic);
            }
        }

        /// <summary>
        /// Handle MQTT connection changes with performance tracking
        /// </summary>
        private void OnMqttConnectionChanged(object sender, bool isConnected)
        {
            try
            {
                if (isConnected)
                {
                    _logger.LogInformation("MQTT broker connection established");
                    _performanceService.RecordMqttMessage("connection", true, "Connected");
                }
                else
                {
                    _logger.LogWarning("MQTT broker connection lost");
                    _performanceService.RecordMqttMessage("connection", false, "Disconnected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking MQTT connection change");
            }
        }
    }
}