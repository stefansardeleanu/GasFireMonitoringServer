// File: Services/DataProcessingService.cs
// MINIMAL FIX: Only changes to work with existing files

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
    public class DataProcessingService
    {
        private readonly ILogger<DataProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<MonitoringHub> _hubContext;

        // Performance counters
        private long _messagesProcessed = 0;
        private long _sensorsUpdated = 0;
        private long _alarmsProcessed = 0;
        private DateTime _lastStatsReport = DateTime.UtcNow;

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

        // This method runs when MQTT message is received
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

                // Parse topic: /PLCNEXT/5_PanouHurezani/CH41
                var topicParts = topic.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                if (topicParts.Length < 3)
                {
                    _logger.LogWarning("Invalid topic format: {Topic}", topic);
                    return;
                }

                var siteInfo = topicParts[1];  // "5_PanouHurezani"
                var channel = topicParts[2];   // "CH41" or "Alarms"

                // Extract site ID and name
                var siteParts = siteInfo.Split('_', 2);
                if (!int.TryParse(siteParts[0], out var siteId))
                {
                    _logger.LogWarning("Could not parse site ID from: {SiteInfo}", siteInfo);
                    return;
                }
                var siteName = siteParts.Length > 1 ? siteParts[1] : "Unknown";

                // Process based on channel type
                if (channel.ToLower() == "alarms" || channel.ToLower() == "alarm")
                {
                    await ProcessAlarm(siteId, siteName, payload);
                    _alarmsProcessed++;
                }
                else if (channel.StartsWith("CH"))
                {
                    await ProcessSensorData(siteId, siteName, channel, payload);
                    _sensorsUpdated++;
                }
                else
                {
                    _logger.LogDebug("Unknown channel type: {Channel} for site {SiteId}", channel, siteId);
                }

                // Report statistics every 10 minutes
                await ReportStatisticsIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message from topic {Topic}",
                    message.Split('|')[0]);
            }
        }

        // Process sensor data messages
        private async Task ProcessSensorData(int siteId, string siteName, string channel, string payload)
        {
            try
            {
                // Parse JSON payload
                var json = JsonDocument.Parse(payload);
                var root = json.RootElement;

                // Extract channel number from "CH41" -> "41"
                var channelId = channel.Substring(2);

                // Read values from JSON
                var currentMa = GetJsonValue(root, $"rCH{channelId}_mA", 0.0);
                var processValue = GetJsonValue(root, $"rCH{channelId}_PV", 0.0);
                var detStatus = GetJsonValue(root, $"iCH{channelId}_DetStatus", 0);
                var tagName = GetJsonString(root, $"strCH{channelId}_TAG", "");
                var detType = GetJsonValue(root, $"iCH{channelId}_DetType", 0);

                // Skip if no tag name
                if (string.IsNullOrEmpty(tagName))
                {
                    _logger.LogDebug("No tag name found for {Channel} at site {SiteId}", channel, siteId);
                    return;
                }

                // Create database scope
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    // Find existing sensor or create new
                    var sensor = dbContext.Sensors
                        .FirstOrDefault(s => s.SiteId == siteId && s.ChannelId == channelId);

                    bool isNewSensor = sensor == null;
                    if (isNewSensor)
                    {
                        sensor = new Sensor
                        {
                            SiteId = siteId,
                            SiteName = siteName,
                            ChannelId = channelId
                        };
                        dbContext.Sensors.Add(sensor);
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
                    sensor.Topic = $"/PLCNEXT/{siteId}_{siteName}/{channel}";
                    sensor.RawJson = payload;

                    // Save to database
                    var changeCount = await dbContext.SaveChangesAsync();

                    if (changeCount > 0)
                    {
                        // Log important events
                        if (isNewSensor)
                        {
                            _logger.LogInformation("New sensor added: {TagName} at site {SiteName} (ID: {SiteId})",
                                tagName, siteName, siteId);
                        }

                        // Log alarm conditions
                        if (detStatus > 0)
                        {
                            _logger.LogWarning("Sensor alarm: {TagName} at {SiteName} - Status: {StatusText}, Value: {ProcessValue} {Units}",
                                tagName, siteName, sensor.StatusText, processValue, sensor.Units);
                        }

                        // Send real-time update to connected clients (using existing method)
                        await MonitoringHub.SendSensorUpdate(_hubContext, siteId, new
                        {
                            id = $"{siteId}_{channelId}",
                            siteId = siteId,
                            tag = tagName,
                            processValue = processValue,
                            status = detStatus,
                            units = sensor.Units,
                            lastUpdate = sensor.LastUpdated
                        });
                    }
                    else
                    {
                        _logger.LogDebug("No database changes for sensor {TagName} at site {SiteName}", tagName, siteName);
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database error processing sensor {TagName} at site {SiteName}",
                        tagName, siteName);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data for {Channel} at site {SiteId}", channel, siteId);
                throw;
            }
        }

        // Process alarm messages
        private async Task ProcessAlarm(int siteId, string siteName, string payload)
        {
            try
            {
                _logger.LogWarning("ALARM received from site {SiteName} (ID: {SiteId}): {Message}",
                    siteName, siteId, payload);

                // Parse alarm format: "DT#2024-11-27-07:28:40.99, Alarm Level 2, Det_01"
                var parts = payload.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 3)
                {
                    _logger.LogWarning("Invalid alarm format from site {SiteId}. Expected 3 parts, got {Count}. Payload: {Payload}",
                        siteId, parts.Length, payload);
                    return;
                }

                var sensorTag = parts[2];
                var alarmDescription = parts[1];

                // Create database scope for alarm logging
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var alarm = new Alarm
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    SensorTag = sensorTag,
                    AlarmMessage = alarmDescription,
                    RawMessage = payload,
                    Timestamp = DateTime.UtcNow
                    // REMOVED: IsAcknowledged = false (doesn't exist in your Alarm entity)
                };

                dbContext.Alarms.Add(alarm);
                await dbContext.SaveChangesAsync();

                // Send real-time alarm notification (using existing method name)
                await MonitoringHub.SendAlarmNotification(_hubContext, siteId, new
                {
                    id = alarm.Id,
                    siteId = siteId,
                    siteName = siteName,
                    sensorTag = sensorTag,
                    message = alarmDescription,
                    timestamp = alarm.Timestamp
                });

                _logger.LogWarning("Alarm logged: {AlarmDescription} for sensor {SensorTag} at {SiteName}",
                    alarmDescription, sensorTag, siteName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm for site {SiteId}", siteId);
            }
        }

        // Report processing statistics periodically
        private async Task ReportStatisticsIfNeeded()
        {
            var now = DateTime.UtcNow;
            if (now - _lastStatsReport > TimeSpan.FromMinutes(10))
            {
                _logger.LogInformation("Processing Statistics: {MessagesProcessed} messages, {SensorsUpdated} sensor updates, {AlarmsProcessed} alarms processed since last report",
                    _messagesProcessed, _sensorsUpdated, _alarmsProcessed);

                _lastStatsReport = now;
                // Reset counters for next period
                _messagesProcessed = 0;
                _sensorsUpdated = 0;
                _alarmsProcessed = 0;
            }
        }

        // Helper method to safely get double values from JSON
        private double GetJsonValue(JsonElement root, string propertyName, double defaultValue)
        {
            try
            {
                if (root.TryGetProperty(propertyName, out var element))
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        if (double.TryParse(element.GetString(), out var result))
                            return result;
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetDouble();
                    }
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing {PropertyName}, using default: {DefaultValue}", propertyName, defaultValue);
                return defaultValue;
            }
        }

        // Helper method to safely get string values from JSON
        private string GetJsonString(JsonElement root, string propertyName, string defaultValue)
        {
            try
            {
                if (root.TryGetProperty(propertyName, out var element))
                {
                    return element.GetString() ?? defaultValue;
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing {PropertyName}, using default: {DefaultValue}", propertyName, defaultValue);
                return defaultValue;
            }
        }

        // Helper method to get status text
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
                _ => $"Unknown Status {status}"
            };
        }

        // Helper method to get units for detector type
        private string GetUnitsForType(int detectorType)
        {
            return detectorType switch
            {
                1 => "%LEL",  // Gas detector
                2 => "units", // Flame detector
                3 => "PPM",   // Toxic gas
                _ => "units"
            };
        }
    }
}