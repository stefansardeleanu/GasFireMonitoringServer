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
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;

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
                _logger.LogInformation($"MQTT message received: {message}");

                // Split topic and payload
                var parts = message.Split('|');
                if (parts.Length != 2)
                {
                    _logger.LogWarning($"Invalid message format. Expected 'topic|payload', got: {message}");
                    return;
                }

                var topic = parts[0];
                var payload = parts[1];

                _logger.LogInformation($"Processing - Topic: {topic}, Payload length: {payload.Length}");

                // Parse topic: /PLCNEXT/5_PanouHurezani/CH41
                var topicParts = topic.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                if (topicParts.Length < 3)
                {
                    _logger.LogWarning($"Invalid topic format. Expected at least 3 parts, got: {topicParts.Length}");
                    return;
                }

                var siteInfo = topicParts[1];  // "5_PanouHurezani"
                var channel = topicParts[2];    // "CH41" or "Alarms"

                // Extract site ID and name
                var siteParts = siteInfo.Split('_', 2);
                if (!int.TryParse(siteParts[0], out var siteId))
                {
                    _logger.LogWarning($"Could not parse site ID from: {siteInfo}");
                    return;
                }
                var siteName = siteParts.Length > 1 ? siteParts[1] : "Unknown";

                _logger.LogInformation($"Parsed - Site: {siteId}_{siteName}, Channel: {channel}");

                // Process based on channel
                if (channel.ToLower() == "alarms" || channel.ToLower() == "alarm")
                {
                    await ProcessAlarm(siteId, siteName, payload);
                }
                else if (channel.StartsWith("CH"))
                {
                    await ProcessSensorData(siteId, siteName, channel, payload);
                }
                else
                {
                    _logger.LogWarning($"Unknown channel type: {channel}");
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

                _logger.LogDebug($"Extracted values - Tag: {tagName}, mA: {currentMa}, PV: {processValue}, Status: {detStatus}, Type: {detType}");

                // Skip if no tag name
                if (string.IsNullOrEmpty(tagName))
                {
                    _logger.LogWarning($"No tag name found for {channel} at site {siteId}");
                    return;
                }

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
                    sensor.TagName = tagName;  // <-- IMPORTANT: Set the tag name!
                    sensor.SiteName = siteName; // <-- Also set site name
                    sensor.DetectorType = (int)(DetectorType)(int)detType;
                    sensor.ProcessValue = processValue;
                    sensor.CurrentValue = currentMa;
                    sensor.Status = (int)(SensorStatus)(int)detStatus;
                    sensor.StatusText = GetStatusText((int)detStatus);
                    sensor.Units = GetUnitsForType((int)detType);
                    sensor.LastUpdated = DateTime.UtcNow;
                    sensor.Topic = $"/PLCNEXT/{siteId}_{siteName}/{channel}"; // Store the topic
                    sensor.RawJson = payload; // Store the raw JSON for debugging

                    // Save to database
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Updated sensor {tagName} at site {siteName} - Status: {sensor.StatusText}");

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
                _logger.LogInformation($"Processing alarm - Site: {siteId}_{siteName}, Payload: {payload}");

                // Parse alarm format: "DT#2024-11-27-07:28:40.99, Alarm Level 2, Det_01"
                var parts = payload.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 3)
                {
                    _logger.LogWarning($"Invalid alarm format. Expected 3 parts, got {parts.Length}. Payload: {payload}");
                    return;
                }

                // Extract sensor tag (third part)
                var sensorTag = parts[2];

                // Extract alarm description (second part - e.g. "Alarm Level 2", "Detector Fault", etc.)
                var alarmDescription = parts[1];

                // Parse timestamp from "DT#2024-11-27-07:28:40.99"
                DateTime alarmTimestamp = DateTime.UtcNow; // Default to now
                var timestampStr = parts[0];
                if (timestampStr.StartsWith("DT#"))
                {
                    try
                    {
                        // Remove "DT#" prefix
                        var dateStr = timestampStr.Substring(3);

                        // Parse the specific format: 2024-11-27-22:34:23.55
                        // Split by dash to separate date and time parts
                        var dateTimeParts = dateStr.Split('-');
                        if (dateTimeParts.Length >= 4)
                        {
                            // Reconstruct in a standard format: "2024-11-27 22:34:23.55"
                            var year = dateTimeParts[0];
                            var month = dateTimeParts[1];
                            var day = dateTimeParts[2];
                            var time = string.Join(":", dateTimeParts.Skip(3)); // Join remaining parts as time

                            var standardFormat = $"{year}-{month}-{day} {time}";
                            alarmTimestamp = DateTime.Parse(standardFormat, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            _logger.LogWarning($"Unexpected timestamp format: {dateStr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not parse alarm timestamp: {timestampStr}. Using current time. Error: {ex.Message}");
                    }
                }

                // Create a new scope for database access
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Create new alarm record
                var alarm = new Alarm
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    SensorTag = sensorTag,
                    AlarmMessage = alarmDescription,  // Just the alarm description part
                    RawMessage = payload,             // Complete message for reference
                    Timestamp = alarmTimestamp
                };

                dbContext.Alarms.Add(alarm);
                await dbContext.SaveChangesAsync();

                _logger.LogWarning($"Alarm recorded: {siteName} - {sensorTag} - {alarmDescription}");

                // Send real-time alarm notification to connected clients
                await MonitoringHub.SendAlarmNotification(_hubContext, siteId, new
                {
                    id = alarm.Id,
                    siteId = siteId,
                    siteName = siteName,
                    sensorTag = sensorTag,
                    alarmMessage = alarmDescription,
                    rawMessage = payload,
                    timestamp = alarm.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing alarm. Payload: {payload}");
            }
        }

        // Helper method to get double value from JSON
        private double GetJsonValue(JsonElement root, string propertyName, double defaultValue)
        {
            if (root.TryGetProperty(propertyName, out var element))
            {
                // Handle different JSON value types
                switch (element.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (element.TryGetDouble(out var doubleValue))
                            return doubleValue;
                        break;

                    case JsonValueKind.String:
                        var stringValue = element.GetString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            // Try to parse as double (handles scientific notation)
                            if (double.TryParse(stringValue,
                                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowExponent,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var parsed))
                            {
                                return parsed;
                            }

                            // Try to parse as integer
                            if (int.TryParse(stringValue, out var intValue))
                            {
                                return intValue;
                            }
                        }
                        break;
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