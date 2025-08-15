// File: Services/DataProcessingService.cs
// OPTIMIZED VERSION - Phase 7.3 Task 2: SignalR Integration Optimization
// Key optimizations: Enhanced broadcasting logic, performance monitoring, selective updates
// Performance improvements: Reduced SignalR traffic, better error handling

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Models.Enums;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;

namespace GasFireMonitoringServer.Services
{
    /// <summary>
    /// OPTIMIZED Data processing service with enhanced SignalR integration
    /// Phase 7.3 Performance Improvements:
    /// - Selective SignalR broadcasting based on data significance
    /// - Enhanced performance monitoring and metrics
    /// - Optimized message payload construction
    /// - Better error handling for real-time communications
    /// </summary>
    public class DataProcessingService
    {
        private readonly ILogger<DataProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<MonitoringHub> _hubContext;

        // OPTIMIZATION: Enhanced performance counters
        private long _messagesProcessed = 0;
        private long _sensorsUpdated = 0;
        private long _alarmsProcessed = 0;
        private long _signalRBroadcasts = 0;
        private long _skippedBroadcasts = 0;
        private DateTime _lastStatsReport = DateTime.UtcNow;

        // OPTIMIZATION: Performance thresholds for selective broadcasting
        private readonly TimeSpan _minBroadcastInterval = TimeSpan.FromSeconds(1); // Prevent spam
        private readonly DateTime _lastBroadcastTimes = DateTime.MinValue;

        public DataProcessingService(
            ILogger<DataProcessingService> logger,
            IServiceProvider serviceProvider,
            IHubContext<MonitoringHub> hubContext,
            IMqttService mqttService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;

            // Subscribe to MQTT messages
            mqttService.MessageReceived += OnMqttMessageReceived;
            _logger.LogInformation("DataProcessingService initialized and subscribed to MQTT events");
        }

        /// <summary>
        /// OPTIMIZED: Enhanced MQTT message processing with selective broadcasting
        /// </summary>
        private async void OnMqttMessageReceived(object sender, string message)
        {
            _messagesProcessed++;

            try
            {
                // Split topic and payload
                var parts = message.Split('|');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid MQTT message format: {Message}", message.Substring(0, Math.Min(100, message.Length)));
                    return;
                }

                var topic = parts[0];
                var payload = parts[1];

                _logger.LogDebug("Processing MQTT message from topic: {Topic}", topic);

                // Process different message types
                if (topic.Contains("/Alarms"))
                {
                    await ProcessAlarmMessage(topic, payload);
                }
                else
                {
                    await ProcessSensorMessage(topic, payload);
                }

                // OPTIMIZATION: Report performance stats periodically
                await ReportPerformanceStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message: {Message}",
                    message.Substring(0, Math.Min(100, message.Length)));
            }
        }

        /// <summary>
        /// OPTIMIZED: Enhanced sensor message processing with selective broadcasting
        /// </summary>
        private async Task ProcessSensorMessage(string topic, string payload)
        {
            try
            {
                // Parse topic: /PLCNEXT/5_PanouHurezani/CH41
                var topicParts = topic.Split('/');
                if (topicParts.Length < 4)
                {
                    _logger.LogWarning("Invalid sensor topic format: {Topic}", topic);
                    return;
                }

                var siteInfo = topicParts[2]; // "5_PanouHurezani"
                var channel = topicParts[3];  // "CH41"

                // Extract site ID and name
                var siteParts = siteInfo.Split('_');
                if (siteParts.Length < 2 || !int.TryParse(siteParts[0], out var siteId))
                {
                    _logger.LogWarning("Invalid site info format: {SiteInfo}", siteInfo);
                    return;
                }

                var siteName = string.Join("_", siteParts.Skip(1));
                var channelId = channel.Replace("CH", "");

                // Parse JSON payload
                JsonDocument root;
                try
                {
                    root = JsonDocument.Parse(payload);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON payload for topic {Topic}: {Payload}",
                        topic, payload.Substring(0, Math.Min(100, payload.Length)));
                    return;
                }

                // Extract sensor values with enhanced error handling
                var currentMa = GetJsonValue(root, $"rCH{channelId}_mA", 0);
                var processValue = GetJsonValue(root, $"rCH{channelId}_PV", 0);
                var detStatus = GetJsonValue(root, $"iCH{channelId}_DetStatus", 0);
                var tagName = GetJsonString(root, $"strCH{channelId}_TAG", "");
                var detType = GetJsonValue(root, $"iCH{channelId}_DetType", 0);

                // Skip if no tag name (sensor not configured)
                if (string.IsNullOrEmpty(tagName))
                {
                    _logger.LogDebug("No tag name found for {Channel} at site {SiteId}", channel, siteId);
                    return;
                }

                // Process sensor data with database operations
                var (isNewSensor, isSignificantChange, previousStatus) = await UpdateSensorDatabase(
                    siteId, siteName, channelId, tagName, currentMa, processValue, detStatus, detType, payload);

                // OPTIMIZATION: Only broadcast significant changes
                if (ShouldBroadcastSensorUpdate(isNewSensor, isSignificantChange, detStatus, previousStatus))
                {
                    await BroadcastSensorUpdate(siteId, channelId, tagName, siteName,
                        currentMa, processValue, detStatus, detType);
                    _signalRBroadcasts++;
                }
                else
                {
                    _skippedBroadcasts++;
                    _logger.LogDebug("Skipped broadcast for sensor {TagName} (no significant change)", tagName);
                }

                _sensorsUpdated++;

                // OPTIMIZATION: Log alarms with enhanced details
                if (detStatus > 0)
                {
                    _logger.LogWarning("Sensor alarm: {TagName} at {SiteName} - Status: {StatusText}, Value: {ProcessValue} {Units}",
                        tagName, siteName, GetStatusText((int)detStatus), processValue, GetUnitsForType((int)detType));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor message from topic {Topic}", topic);
            }
        }

        /// <summary>
        /// OPTIMIZED: Enhanced database update with change detection
        /// </summary>
        private async Task<(bool isNewSensor, bool isSignificantChange, int previousStatus)> UpdateSensorDatabase(
            int siteId, string siteName, string channelId, string tagName,
            double currentMa, double processValue, double detStatus, double detType, string payload)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Find existing sensor or create new
                var sensor = dbContext.Sensors
                    .FirstOrDefault(s => s.SiteId == siteId && s.ChannelId == channelId);

                bool isNewSensor = sensor == null;
                int previousStatus = 0;
                bool isSignificantChange = false;

                if (isNewSensor)
                {
                    sensor = new Sensor
                    {
                        SiteId = siteId,
                        SiteName = siteName,
                        ChannelId = channelId
                    };
                    dbContext.Sensors.Add(sensor);
                    isSignificantChange = true; // New sensors are always significant
                }
                else
                {
                    // OPTIMIZATION: Track what actually changed
                    previousStatus = sensor.Status;
                    isSignificantChange =
                        Math.Abs(sensor.ProcessValue - processValue) > 0.1 || // Value changed significantly
                        sensor.Status != (int)detStatus || // Status changed
                        (DateTime.UtcNow - sensor.LastUpdated).TotalMinutes > 5; // Haven't updated in 5 minutes
                }

                // Update sensor values
                sensor.TagName = tagName;
                sensor.SiteName = siteName;
                sensor.DetectorType = (int)(DetectorType)(int)detType;
                sensor.ProcessValue = processValue;
                sensor.CurrentValue = currentMa;
                sensor.Status = (int)(SensorStatus)(int)detStatus;
                sensor.StatusText = GetStatusText((int)detStatus);
                sensor.Units = GetUnitsForType((int)detType);
                sensor.LastUpdated = DateTime.UtcNow;
                sensor.Topic = $"/PLCNEXT/{siteId}_{siteName}/{channelId}";
                sensor.RawJson = payload;

                // Save to database
                var changeCount = await dbContext.SaveChangesAsync();

                if (changeCount > 0 && isNewSensor)
                {
                    _logger.LogInformation("New sensor added: {TagName} at site {SiteName} (ID: {SiteId})",
                        tagName, siteName, siteId);
                }

                return (isNewSensor, isSignificantChange, previousStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor database for {TagName} at site {SiteId}", tagName, siteId);
                return (false, false, 0);
            }
        }

        /// <summary>
        /// OPTIMIZATION: Intelligent broadcasting decision logic
        /// </summary>
        private bool ShouldBroadcastSensorUpdate(bool isNewSensor, bool isSignificantChange,
            double currentStatus, int previousStatus)
        {
            // Always broadcast new sensors
            if (isNewSensor) return true;

            // Always broadcast status changes (especially alarms)
            if (currentStatus != previousStatus) return true;

            // Always broadcast alarm conditions
            if (currentStatus > 0) return true;

            // Broadcast significant value changes
            return isSignificantChange;
        }

        /// <summary>
        /// OPTIMIZED: Enhanced sensor update broadcasting with optimized payload
        /// </summary>
        private async Task BroadcastSensorUpdate(int siteId, string channelId, string tagName,
            string siteName, double currentMa, double processValue, double detStatus, double detType)
        {
            try
            {
                // OPTIMIZATION: Create optimized payload with only essential data
                var sensorUpdate = new
                {
                    id = $"{siteId}_{channelId}",
                    siteId,
                    siteName,
                    channelId,
                    tagName,
                    processValue = Math.Round(processValue, 2),
                    currentMa = Math.Round(currentMa, 2),
                    status = (int)detStatus,
                    statusText = GetStatusText((int)detStatus),
                    detectorType = (int)detType,
                    units = GetUnitsForType((int)detType),
                    timestamp = DateTime.UtcNow,
                    isAlarm = detStatus > 0
                };

                // Use optimized SignalR broadcasting
                await MonitoringHub.SendSensorUpdate(_hubContext, siteId, sensorUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting sensor update for {TagName} at site {SiteId}", tagName, siteId);
            }
        }

        /// <summary>
        /// OPTIMIZED: Enhanced alarm message processing
        /// </summary>
        private async Task ProcessAlarmMessage(string topic, string payload)
        {
            try
            {
                _alarmsProcessed++;

                // Parse topic: /PLCNEXT/5_PanouHurezani/Alarms
                var topicParts = topic.Split('/');
                if (topicParts.Length < 3)
                {
                    _logger.LogWarning("Invalid alarm topic format: {Topic}", topic);
                    return;
                }

                var siteInfo = topicParts[2]; // "5_PanouHurezani"
                var siteParts = siteInfo.Split('_');

                if (siteParts.Length < 2 || !int.TryParse(siteParts[0], out var siteId))
                {
                    _logger.LogWarning("Invalid alarm site info format: {SiteInfo}", siteInfo);
                    return;
                }

                var siteName = string.Join("_", siteParts.Skip(1));

                // Parse alarm data
                var alarmData = await ProcessAlarmData(siteId, siteName, payload);
                if (alarmData != null)
                {
                    // OPTIMIZATION: Always broadcast alarms (critical for safety)
                    await MonitoringHub.SendAlarmNotification(_hubContext, siteId, alarmData);
                    _signalRBroadcasts++;

                    _logger.LogWarning("Alarm broadcast: Site {SiteId} ({SiteName}) - {AlarmMessage}",
                        siteId, siteName, alarmData.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm message from topic {Topic}", topic);
            }
        }

        /// <summary>
        /// OPTIMIZED: Enhanced alarm data processing
        /// </summary>
        private async Task<object?> ProcessAlarmData(int siteId, string siteName, string payload)
        {
            try
            {
                // Parse JSON payload
                JsonDocument root;
                try
                {
                    root = JsonDocument.Parse(payload);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON alarm payload: {Payload}",
                        payload.Substring(0, Math.Min(100, payload.Length)));
                    return null;
                }

                // Extract alarm information
                var timestamp = DateTime.UtcNow;
                var alarmMessage = GetJsonString(root, "message", "Unknown alarm");
                var sensorTag = GetJsonString(root, "sensorTag", "");
                var severity = GetJsonString(root, "severity", "medium");
                var alarmType = GetJsonString(root, "type", "sensor");

                // OPTIMIZATION: Create alarm record in database
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var alarm = new Alarm
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    SensorTag = sensorTag,
                    AlarmMessage = alarmMessage,
                    Timestamp = timestamp,
                    RawMessage = payload
                };

                dbContext.Alarms.Add(alarm);
                await dbContext.SaveChangesAsync();

                // Return optimized alarm data for broadcasting
                return new
                {
                    id = alarm.Id,
                    siteId,
                    siteName,
                    sensorTag,
                    message = alarmMessage,
                    severity,
                    type = alarmType,
                    timestamp,
                    isActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm data for site {SiteId}", siteId);
                return null;
            }
        }

        /// <summary>
        /// OPTIMIZED: Performance monitoring and reporting
        /// </summary>
        private async Task ReportPerformanceStats()
        {
            var now = DateTime.UtcNow;

            // Report stats every 5 minutes
            if ((now - _lastStatsReport).TotalMinutes >= 5)
            {
                _lastStatsReport = now;

                var statsMessage = $"DataProcessing Performance Stats - " +
                    $"Messages: {_messagesProcessed}, " +
                    $"Sensors Updated: {_sensorsUpdated}, " +
                    $"Alarms: {_alarmsProcessed}, " +
                    $"SignalR Broadcasts: {_signalRBroadcasts}, " +
                    $"Skipped Broadcasts: {_skippedBroadcasts}, " +
                    $"Broadcast Efficiency: {CalculateBroadcastEfficiency():P1}";

                _logger.LogInformation(statsMessage);

                // OPTIMIZATION: Broadcast system stats to monitoring clients
                try
                {
                    var systemStats = new
                    {
                        messagesProcessed = _messagesProcessed,
                        sensorsUpdated = _sensorsUpdated,
                        alarmsProcessed = _alarmsProcessed,
                        signalRBroadcasts = _signalRBroadcasts,
                        skippedBroadcasts = _skippedBroadcasts,
                        broadcastEfficiency = CalculateBroadcastEfficiency(),
                        timestamp = now,
                        period = "5min"
                    };

                    await _hubContext.Clients.Group("system_monitoring")
                        .SendAsync("SystemPerformanceStats", systemStats);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting system performance stats");
                }
            }
        }

        /// <summary>
        /// OPTIMIZATION: Calculate broadcast efficiency metric
        /// </summary>
        private double CalculateBroadcastEfficiency()
        {
            var totalUpdates = _signalRBroadcasts + _skippedBroadcasts;
            return totalUpdates > 0 ? (double)_signalRBroadcasts / totalUpdates : 0;
        }

        #region Helper Methods (Preserved from original)

        private double GetJsonValue(JsonDocument document, string propertyName, double defaultValue)
        {
            try
            {
                if (document.RootElement.TryGetProperty(propertyName, out var element))
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var stringValue = element.GetString();
                        if (double.TryParse(stringValue, out var parsedValue))
                            return parsedValue;
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetDouble();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing JSON property {PropertyName}", propertyName);
            }

            return defaultValue;
        }

        private string GetJsonString(JsonDocument document, string propertyName, string defaultValue)
        {
            try
            {
                if (document.RootElement.TryGetProperty(propertyName, out var element))
                {
                    return element.GetString() ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing JSON string property {PropertyName}", propertyName);
            }

            return defaultValue;
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Normal",
                1 => "Alarm Level 1",
                2 => "Alarm Level 2",
                3 => "Detector Error",
                4 => "Detector Disabled",
                5 => "Line Open Fault",
                6 => "Line Short Fault",
                _ => $"Unknown ({status})"
            };
        }

        private string GetUnitsForType(int detectorType)
        {
            return detectorType switch
            {
                1 => "%LEL", // Gas detector
                2 => "IR",   // Flame detector
                3 => "ppm",  // Toxic gas
                4 => "°C",   // Temperature
                _ => ""
            };
        }

        #endregion
    }
}