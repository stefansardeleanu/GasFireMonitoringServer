// File: Services/DataProcessingService.cs
// This service processes MQTT messages and updates the database

using System;
using System.Linq;
using System.Text.Json;  // For parsing JSON
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Models.Enums;
using GasFireMonitoringServer.Services.Interfaces;
using GasFireMonitoringServer.Hubs;

namespace GasFireMonitoringServer.Services
{
    public class DataProcessingService
    {
        private readonly ILogger<DataProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<MonitoringHub> _hubContext;

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
        }

        // This method runs when MQTT message is received
        private async void OnMqttMessageReceived(object sender, string message)
        {
            try
            {
                // Split topic and payload
                var parts = message.Split('|');
                if (parts.Length != 2) return;

                var topic = parts[0];
                var payload = parts[1];

                // Parse topic: /PLCNEXT/5_PanouHurezani/CH41
                var topicParts = topic.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                if (topicParts.Length < 3) return;

                var siteInfo = topicParts[1];  // "5_PanouHurezani"
                var channel = topicParts[2];    // "CH41" or "Alarms"

                // Extract site ID and name
                var siteParts = siteInfo.Split('_', 2);
                var siteId = int.Parse(siteParts[0]);
                var siteName = siteParts.Length > 1 ? siteParts[1] : "Unknown";

                // Process based on channel
                if (channel.ToLower() == "alarms")
                {
                    await ProcessAlarm(siteId, siteName, payload);
                }
                else if (channel.StartsWith("CH"))
                {
                    await ProcessSensorData(siteId, siteName, channel, payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }
        }

        // Process sensor data messages
        private async Task ProcessSensorData(int siteId, string siteName, string channel, string payload)
        {
            try
            {
                // Log the raw payload for debugging
                _logger.LogDebug($"Processing {channel} - Payload: {payload}");

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
                if (string.IsNullOrEmpty(tagName)) return;

                // Create a new scope for database access
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    // Find existing sensor or create new
                    var sensor = dbContext.Sensors
                        .FirstOrDefault(s => s.SiteId == siteId && s.ChannelId == channelId);

                    if (sensor == null)
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
                    // Cast doubles to integers for enum properties and integer parameters
                    sensor.DetectorType = (int)(DetectorType)(int)detType;
                    sensor.ProcessValue = processValue;
                    sensor.CurrentValue = currentMa;
                    sensor.Status = (int)(SensorStatus)(int)detStatus; // <-- FIXED LINE
                    sensor.StatusText = GetStatusText((int)detStatus);
                    sensor.Units = GetUnitsForType((int)detType);
                    sensor.LastUpdated = DateTime.UtcNow;

                    // Save to database
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Updated sensor {tagName} - Value: {processValue} {sensor.Units}");

                    // Send real-time update to connected clients
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
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database error while processing sensor data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing sensor data for {channel}");
            }
        }

        // Process alarm messages
        private async Task ProcessAlarm(int siteId, string siteName, string payload)
        {
            try
            {
                // Parse alarm format: "DT#2024-11-02-23:01:15.10, Alarm Level 1, KGD-004"
                var parts = payload.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 3) return;

                var alarmText = parts[1];
                var sensorTag = parts[2];

                // Determine alarm level
                var alarmLevel = alarmText.Contains("Level 2") ? 2 : 1;
                var alarmType = alarmLevel == 2 ? SensorStatus.AlarmLevel2 : SensorStatus.AlarmLevel1;

                // Create a new scope for database access
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Create new alarm record
                var alarm = new Alarm
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    SensorTag = sensorTag,
                    AlarmType = alarmType,
                    AlarmLevel = alarmLevel,
                    Timestamp = DateTime.UtcNow,
                    RawMessage = payload
                };

                dbContext.Alarms.Add(alarm);
                await dbContext.SaveChangesAsync();

                _logger.LogWarning($"Alarm recorded: {siteName} - {sensorTag} - Level {alarmLevel}");

                // Send real-time alarm notification to connected clients
                await MonitoringHub.SendAlarmNotification(_hubContext, siteId, new
                {
                    id = alarm.Id,
                    siteId = siteId,
                    siteName = siteName,
                    sensorTag = sensorTag,
                    alarmLevel = alarmLevel,
                    timestamp = alarm.Timestamp,
                    message = payload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm");
            }
        }

        // Helper method to get double value from JSON
        private double GetJsonValue(JsonElement root, string propertyName, double defaultValue)
        {
            if (root.TryGetProperty(propertyName, out var element))
            {
                // First try to get as double directly
                if (element.TryGetDouble(out var value))
                    return value;

                // If it's a string, try to parse it (handles scientific notation)
                if (element.ValueKind == JsonValueKind.String)
                {
                    var stringValue = element.GetString();
                    if (!string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue, out var parsed))
                        return parsed;
                }
            }
            return defaultValue;
        }

        // Helper method to get string value from JSON
        private string GetJsonString(JsonElement root, string propertyName, string defaultValue)
        {
            if (root.TryGetProperty(propertyName, out var element))
            {
                return element.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        // Get units based on detector type
        private string GetUnitsForType(int type)
        {
            return type switch
            {
                1 => "%LEL",
                2 => "PPM",
                3 => "mA",
                4 => "mA",
                5 => "mA",
                _ => ""
            };
        }

        // Get status text based on status code
        private string GetStatusText(int statusCode)
        {
            return statusCode switch
            {
                0 => "Normal",
                1 => "AlarmLevel1",
                2 => "AlarmLevel2",
                3 => "DetectorError",
                4 => "LineOpenFault",
                5 => "LineShortFault",
                6 => "Calibrating",
                7 => "Maintenance",
                8 => "Disabled",
                9 => "Testing",
                10 => "Unknown",
                _ => $"Status{statusCode}"
            };
        }
    }
}