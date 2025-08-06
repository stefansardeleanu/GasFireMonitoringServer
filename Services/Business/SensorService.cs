// File: Services/Business/SensorService.cs
// Business logic implementation for sensor operations

using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Business logic service for sensor operations
    /// Coordinates between repository layer and controllers
    /// Implements all sensor-related business rules
    /// </summary>
    public class SensorService : ISensorService
    {
        private readonly ISensorRepository _sensorRepository;
        private readonly ILogger<SensorService> _logger;

        // Business constants
        private const int ONLINE_THRESHOLD_MINUTES = 5; // Sensors offline after 5 minutes

        public SensorService(ISensorRepository sensorRepository, ILogger<SensorService> logger)
        {
            _sensorRepository = sensorRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors with business logic applied
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetAllSensorsAsync()
        {
            try
            {
                _logger.LogDebug("Getting all sensors with business logic");

                var sensors = await _sensorRepository.GetAllAsync();
                return ApplyBusinessLogicToSensors(sensors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sensors");
                throw;
            }
        }

        /// <summary>
        /// Get sensors for a specific site with business logic
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetSensorsBySiteAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting sensors for site {SiteId} with business logic", siteId);

                var sensors = await _sensorRepository.GetBySiteIdAsync(siteId);
                return ApplyBusinessLogicToSensors(sensors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensors for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get a specific sensor by ID
        /// </summary>
        public async Task<Sensor?> GetSensorByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting sensor {SensorId} with business logic", id);

                var sensor = await _sensorRepository.GetByIdAsync(id);
                return sensor != null ? ApplyBusinessLogicToSensor(sensor) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor {SensorId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get sensors currently in alarm state (Status = 1 or 2)
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetAlarmedSensorsAsync()
        {
            try
            {
                _logger.LogDebug("Getting alarmed sensors with business context");

                var sensors = await _sensorRepository.GetSensorsInAlarmAsync();
                return ApplyBusinessLogicToSensors(sensors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarmed sensors");
                throw;
            }
        }

        /// <summary>
        /// Get comprehensive sensor statistics for a site
        /// </summary>
        public async Task<SensorStats> GetSensorStatsAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Calculating sensor statistics for site {SiteId}", siteId);

                var sensors = await _sensorRepository.GetBySiteIdAsync(siteId);
                var sensorsList = sensors.ToList();

                var stats = new SensorStats
                {
                    TotalSensors = sensorsList.Count,
                    NormalSensors = sensorsList.Count(s => s.Status == 0),
                    AlarmSensors = sensorsList.Count(s => s.Status == 1 || s.Status == 2),
                    FaultSensors = sensorsList.Count(s => s.Status == 3 || s.Status == 5 || s.Status == 6),
                    DisabledSensors = sensorsList.Count(s => s.Status == 4),
                    OnlineSensors = sensorsList.Count(s => IsOnline(s.LastUpdated)),
                    OfflineSensors = sensorsList.Count(s => !IsOnline(s.LastUpdated)),
                    LastUpdate = sensorsList.Any() ? sensorsList.Max(s => s.LastUpdated) : null,
                    SensorTypeBreakdown = sensorsList.GroupBy(s => s.DetectorType)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                _logger.LogInformation("Site {SiteId} stats: {Total} total, {Normal} normal, {Alarm} alarm, {Fault} fault, {Disabled} disabled",
                    siteId, stats.TotalSensors, stats.NormalSensors, stats.AlarmSensors, stats.FaultSensors, stats.DisabledSensors);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sensor statistics for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Process a sensor update from MQTT
        /// </summary>
        public async Task<Sensor> ProcessSensorUpdateAsync(Sensor sensor)
        {
            try
            {
                _logger.LogDebug("Processing sensor update for {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);

                // Apply business rules for sensor updates
                sensor = ApplyBusinessLogicToSensor(sensor);

                // Update in repository
                var existingSensor = await _sensorRepository.GetByIdAsync(sensor.Id);
                if (existingSensor != null)
                {
                    return await _sensorRepository.UpdateAsync(sensor);
                }
                else
                {
                    return await _sensorRepository.CreateAsync(sensor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor update for {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);
                throw;
            }
        }

        /// <summary>
        /// Get sensor type breakdown for all sites
        /// </summary>
        public async Task<Dictionary<int, Dictionary<int, int>>> GetSensorTypeBreakdownAsync()
        {
            try
            {
                _logger.LogDebug("Getting sensor type breakdown for all sites");

                var allSensors = await _sensorRepository.GetAllAsync();

                return allSensors
                    .GroupBy(s => s.SiteId)
                    .ToDictionary(
                        siteGroup => siteGroup.Key,
                        siteGroup => siteGroup
                            .GroupBy(s => s.DetectorType)
                            .ToDictionary(typeGroup => typeGroup.Key, typeGroup => typeGroup.Count())
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor type breakdown");
                throw;
            }
        }

        /// <summary>
        /// Determine if sensors at a site are online
        /// Business rule: online = updated within last 5 minutes
        /// </summary>
        public async Task<bool> IsSiteOnlineAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Checking if site {SiteId} is online", siteId);

                var lastUpdate = await _sensorRepository.GetLastUpdateTimeAsync(siteId);

                if (!lastUpdate.HasValue)
                {
                    _logger.LogDebug("Site {SiteId} has no sensor data - considered offline", siteId);
                    return false;
                }

                var isOnline = IsOnline(lastUpdate.Value);
                _logger.LogDebug("Site {SiteId} online status: {IsOnline} (last update: {LastUpdate})",
                    siteId, isOnline, lastUpdate.Value);

                return isOnline;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if site {SiteId} is online", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get sensors with specific status
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetSensorsByStatusAsync(int status)
        {
            try
            {
                _logger.LogDebug("Getting sensors with status {Status}", status);

                var allSensors = await _sensorRepository.GetAllAsync();
                var filteredSensors = allSensors.Where(s => s.Status == status);

                return ApplyBusinessLogicToSensors(filteredSensors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensors with status {Status}", status);
                throw;
            }
        }

        /// <summary>
        /// Get overall system sensor statistics
        /// </summary>
        public async Task<SensorStats> GetSystemSensorStatsAsync()
        {
            try
            {
                _logger.LogDebug("Calculating system-wide sensor statistics");

                var allSensors = await _sensorRepository.GetAllAsync();
                var sensorsList = allSensors.ToList();

                var stats = new SensorStats
                {
                    TotalSensors = sensorsList.Count,
                    NormalSensors = sensorsList.Count(s => s.Status == 0),
                    AlarmSensors = sensorsList.Count(s => s.Status == 1 || s.Status == 2),
                    FaultSensors = sensorsList.Count(s => s.Status == 3 || s.Status == 5 || s.Status == 6),
                    DisabledSensors = sensorsList.Count(s => s.Status == 4),
                    OnlineSensors = sensorsList.Count(s => IsOnline(s.LastUpdated)),
                    OfflineSensors = sensorsList.Count(s => !IsOnline(s.LastUpdated)),
                    LastUpdate = sensorsList.Any() ? sensorsList.Max(s => s.LastUpdated) : null,
                    SensorTypeBreakdown = sensorsList.GroupBy(s => s.DetectorType)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system sensor statistics");
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Apply business logic to a collection of sensors
        /// </summary>
        private IEnumerable<Sensor> ApplyBusinessLogicToSensors(IEnumerable<Sensor> sensors)
        {
            return sensors.Select(ApplyBusinessLogicToSensor);
        }

        /// <summary>
        /// Apply business logic to a single sensor
        /// Updates computed properties and status text
        /// </summary>
        private Sensor ApplyBusinessLogicToSensor(Sensor sensor)
        {
            // Update status text based on business rules
            sensor.StatusText = GetStatusText(sensor.Status);

            // Any other business logic transformations can be added here

            return sensor;
        }

        /// <summary>
        /// Get human-readable status text
        /// Business rule for status interpretation
        /// </summary>
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
                _ => $"Unknown Status ({status})"
            };
        }

        /// <summary>
        /// Determine if a sensor is online based on last update time
        /// Business rule: online = updated within last 5 minutes
        /// </summary>
        private bool IsOnline(DateTime lastUpdated)
        {
            return (DateTime.UtcNow - lastUpdated).TotalMinutes <= ONLINE_THRESHOLD_MINUTES;
        }

        #endregion
    }
}