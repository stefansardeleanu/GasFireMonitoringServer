// File: Services/Business/AlarmService.cs
// Business logic implementation for alarm operations

using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Business logic service for alarm operations
    /// Handles alarm processing, statistics, and business rules
    /// </summary>
    public class AlarmService : IAlarmService
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly ILogger<AlarmService> _logger;

        public AlarmService(
            IAlarmRepository alarmRepository,
            ISensorRepository sensorRepository,
            ILogger<AlarmService> logger)
        {
            _alarmRepository = alarmRepository;
            _sensorRepository = sensorRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get alarms with business filtering and context
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetAlarmsAsync(AlarmFilter filter)
        {
            try
            {
                _logger.LogDebug("Getting alarms with business filter - SiteId: {SiteId}, ActiveOnly: {ActiveOnly}",
                    filter.SiteId, filter.ActiveOnly);

                IEnumerable<Alarm> alarms;

                if (filter.ActiveOnly)
                {
                    alarms = await _alarmRepository.GetActiveAlarmsAsync(filter.SiteId);
                }
                else if (!string.IsNullOrEmpty(filter.SensorTag))
                {
                    // Calculate days from date range or use default
                    var days = filter.StartDate.HasValue && filter.EndDate.HasValue
                        ? (int)(filter.EndDate.Value - filter.StartDate.Value).TotalDays
                        : 7;

                    alarms = await _alarmRepository.GetBySensorTagAsync(filter.SensorTag, days);
                }
                else
                {
                    alarms = await _alarmRepository.GetAllAsync(
                        filter.SiteId,
                        filter.StartDate,
                        filter.EndDate,
                        filter.Limit);
                }

                return ApplyBusinessLogicToAlarms(alarms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarms with filter");
                throw;
            }
        }

        /// <summary>
        /// Get alarms for a specific site with business logic
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetAlarmsBySiteAsync(int siteId, int limit = 50)
        {
            try
            {
                _logger.LogDebug("Getting alarms for site {SiteId} with business logic", siteId);

                var alarms = await _alarmRepository.GetBySiteIdAsync(siteId, limit);
                return ApplyBusinessLogicToAlarms(alarms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarms for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get currently active alarms (last 24 hours)
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(int? siteId = null)
        {
            try
            {
                _logger.LogDebug("Getting active alarms for site {SiteId}", siteId);

                var alarms = await _alarmRepository.GetActiveAlarmsAsync(siteId);
                return ApplyBusinessLogicToAlarms(alarms).OrderByDescending(a => a.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active alarms for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get comprehensive alarm statistics
        /// </summary>
        public async Task<AlarmStats> GetAlarmStatsAsync(int? siteId = null)
        {
            try
            {
                _logger.LogDebug("Calculating alarm statistics for site {SiteId}", siteId);

                // Get basic statistics from repository
                var basicStats = await _alarmRepository.GetAlarmStatisticsAsync(siteId);

                // Get additional business statistics
                var last7DaysTrends = await _alarmRepository.GetAlarmCountsByDayAsync(siteId, 7);
                var alarmTypeFrequency = siteId.HasValue
                    ? await _alarmRepository.GetAlarmTypeFrequencyAsync(siteId.Value, 30)
                    : new Dictionary<string, int>();

                // Calculate business metrics
                var totalAlarms = await _alarmRepository.CountAsync(siteId, 365); // Last year
                var alarmsPerDay = totalAlarms > 0 ? (double)totalAlarms / 365 : 0;

                var stats = new AlarmStats
                {
                    TotalAlarms = totalAlarms,
                    Last24Hours = await _alarmRepository.CountAsync(siteId, 1),
                    Last7Days = await _alarmRepository.CountAsync(siteId, 7),
                    Last30Days = await _alarmRepository.CountAsync(siteId, 30),
                    AlarmTypeFrequency = alarmTypeFrequency,
                    DailyTrends = last7DaysTrends,
                    AlarmsPerDay = Math.Round(alarmsPerDay, 2),
                    MostFrequentAlarmTypes = alarmTypeFrequency
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(5)
                        .Select(kvp => $"{kvp.Key} ({kvp.Value})")
                        .ToList()
                };

                // Get oldest and newest from recent alarms
                var recentAlarms = await _alarmRepository.GetAllAsync(siteId, DateTime.UtcNow.AddDays(-30), null, 1000);
                if (recentAlarms.Any())
                {
                    stats.OldestAlarm = recentAlarms.Min(a => a.Timestamp);
                    stats.NewestAlarm = recentAlarms.Max(a => a.Timestamp);
                }

                _logger.LogInformation("Alarm stats for site {SiteId}: {Total} total, {Recent} in 24h, {AlarmsPerDay} per day",
                    siteId, stats.TotalAlarms, stats.Last24Hours, stats.AlarmsPerDay);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating alarm statistics for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Create a new alarm with business validation
        /// </summary>
        public async Task<Alarm> CreateAlarmAsync(int siteId, string siteName, string sensorTag, string alarmMessage, string rawMessage)
        {
            try
            {
                _logger.LogDebug("Creating alarm for site {SiteId}, sensor {SensorTag}", siteId, sensorTag);

                // Business validation
                if (string.IsNullOrWhiteSpace(sensorTag))
                {
                    throw new ArgumentException("Sensor tag cannot be empty", nameof(sensorTag));
                }

                if (string.IsNullOrWhiteSpace(alarmMessage))
                {
                    throw new ArgumentException("Alarm message cannot be empty", nameof(alarmMessage));
                }

                var alarm = new Alarm
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    SensorTag = sensorTag,
                    AlarmMessage = alarmMessage,
                    RawMessage = rawMessage,
                    Timestamp = DateTime.UtcNow
                };

                var createdAlarm = await _alarmRepository.CreateAsync(alarm);

                _logger.LogInformation("Created alarm {AlarmId} for sensor {SensorTag} at site {SiteId}: {AlarmMessage}",
                    createdAlarm.Id, sensorTag, siteId, alarmMessage);

                return createdAlarm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alarm for site {SiteId}, sensor {SensorTag}", siteId, sensorTag);
                throw;
            }
        }

        /// <summary>
        /// Get alarm history for a specific sensor
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetSensorAlarmHistoryAsync(string sensorTag, int days = 7)
        {
            try
            {
                _logger.LogDebug("Getting alarm history for sensor {SensorTag}, last {Days} days", sensorTag, days);

                var alarms = await _alarmRepository.GetBySensorTagAsync(sensorTag, days);
                return ApplyBusinessLogicToAlarms(alarms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm history for sensor {SensorTag}", sensorTag);
                throw;
            }
        }

        /// <summary>
        /// Get alarm trend analysis for dashboard
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetAlarmTrendsAsync(int? siteId = null, int days = 7)
        {
            try
            {
                _logger.LogDebug("Getting alarm trends for site {SiteId}, last {Days} days", siteId, days);

                return await _alarmRepository.GetAlarmCountsByDayAsync(siteId, days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm trends for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get most problematic sensors (frequent alarms)
        /// </summary>
        public async Task<IEnumerable<object>> GetProblematicSensorsAsync(int siteId, int days = 30, int limit = 10)
        {
            try
            {
                _logger.LogDebug("Finding problematic sensors for site {SiteId}, last {Days} days", siteId, days);

                // Get all alarms for the site in the timeframe
                var filter = new AlarmFilter
                {
                    SiteId = siteId,
                    StartDate = DateTime.UtcNow.AddDays(-days),
                    Limit = 10000
                };

                var alarms = await GetAlarmsAsync(filter);

                // Group by sensor and count alarms
                var problematicSensors = alarms
                    .GroupBy(a => a.SensorTag)
                    .Select(g => new
                    {
                        SensorTag = g.Key,
                        AlarmCount = g.Count(),
                        LastAlarm = g.Max(a => a.Timestamp),
                        MostCommonAlarm = g.GroupBy(a => a.AlarmMessage)
                            .OrderByDescending(mg => mg.Count())
                            .First().Key
                    })
                    .OrderByDescending(s => s.AlarmCount)
                    .Take(limit);

                return problematicSensors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting problematic sensors for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Determine alarm severity based on business rules
        /// </summary>
        public string DetermineAlarmSeverity(string alarmMessage)
        {
            if (string.IsNullOrWhiteSpace(alarmMessage))
                return "Low";

            var message = alarmMessage.ToLowerInvariant();

            // Critical alarms - require immediate attention
            if (message.Contains("level 2") || message.Contains("high alarm") || message.Contains("critical"))
                return "Critical";

            // High severity - significant issues
            if (message.Contains("level 1") || message.Contains("alarm") || message.Contains("gas detected"))
                return "High";

            // Medium severity - equipment issues
            if (message.Contains("fault") || message.Contains("error") || message.Contains("malfunction"))
                return "Medium";

            // Low severity - maintenance or information
            return "Low";
        }

        /// <summary>
        /// Check if there are critical alarms requiring immediate attention
        /// </summary>
        public async Task<bool> HasCriticalAlarmsAsync(int? siteId = null)
        {
            try
            {
                _logger.LogDebug("Checking for critical alarms at site {SiteId}", siteId);

                var activeAlarms = await _alarmRepository.GetActiveAlarmsAsync(siteId);

                return activeAlarms.Any(alarm => DetermineAlarmSeverity(alarm.AlarmMessage) == "Critical");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for critical alarms at site {SiteId}", siteId);
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Apply business logic to a collection of alarms
        /// </summary>
        private IEnumerable<Alarm> ApplyBusinessLogicToAlarms(IEnumerable<Alarm> alarms)
        {
            return alarms.Select(ApplyBusinessLogicToAlarm);
        }

        /// <summary>
        /// Apply business logic to a single alarm
        /// Adds computed properties and business context
        /// </summary>
        private Alarm ApplyBusinessLogicToAlarm(Alarm alarm)
        {
            // Add any business logic transformations here
            // For now, alarms are used as-is since they're display-only

            return alarm;
        }

        #endregion
    }
}