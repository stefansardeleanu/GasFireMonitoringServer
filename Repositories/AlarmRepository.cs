// File: Repositories/AlarmRepository.cs
// Concrete implementation of IAlarmRepository
// Handles all alarm database operations

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Repositories
{
    /// <summary>
    /// Repository implementation for alarm data operations
    /// Pure data access layer - no business logic
    /// </summary>
    public class AlarmRepository : IAlarmRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AlarmRepository> _logger;

        public AlarmRepository(ApplicationDbContext context, ILogger<AlarmRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all alarms with optional filtering
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetAllAsync(int? siteId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            try
            {
                _logger.LogDebug("Retrieving alarms with filters - SiteId: {SiteId}, StartDate: {StartDate}, EndDate: {EndDate}, Limit: {Limit}",
                    siteId, startDate, endDate, limit);

                var query = _context.Alarms.AsQueryable();

                // Apply site filter if provided
                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                // Apply date range filters if provided
                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                // Order by most recent first and apply limit
                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms with filters");
                throw;
            }
        }

        /// <summary>
        /// Get alarms for a specific site
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetBySiteIdAsync(int siteId, int limit = 50)
        {
            try
            {
                _logger.LogDebug("Retrieving alarms for site {SiteId} with limit {Limit}", siteId, limit);

                return await _context.Alarms
                    .Where(a => a.SiteId == siteId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms for site {SiteId}", siteId);
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
                _logger.LogDebug("Retrieving active alarms for site {SiteId}", siteId);

                var cutoffTime = DateTime.UtcNow.AddHours(-24); // Last 24 hours
                var query = _context.Alarms.Where(a => a.Timestamp >= cutoffTime);

                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                return await query
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alarms for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get alarms for a specific sensor
        /// </summary>
        public async Task<IEnumerable<Alarm>> GetBySensorTagAsync(string sensorTag, int days = 7)
        {
            try
            {
                _logger.LogDebug("Retrieving alarms for sensor {SensorTag} for last {Days} days", sensorTag, days);

                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                return await _context.Alarms
                    .Where(a => a.SensorTag == sensorTag && a.Timestamp >= cutoffDate)
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms for sensor {SensorTag}", sensorTag);
                throw;
            }
        }

        /// <summary>
        /// Create a new alarm record
        /// </summary>
        public async Task<Alarm> CreateAsync(Alarm alarm)
        {
            try
            {
                _logger.LogDebug("Creating new alarm for site {SiteId}, sensor {SensorTag}",
                    alarm.SiteId, alarm.SensorTag);

                _context.Alarms.Add(alarm);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created alarm with ID {AlarmId} for sensor {SensorTag}",
                    alarm.Id, alarm.SensorTag);
                return alarm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alarm for site {SiteId}, sensor {SensorTag}",
                    alarm.SiteId, alarm.SensorTag);
                throw;
            }
        }

        /// <summary>
        /// Update an existing alarm record
        /// </summary>
        public async Task<Alarm> UpdateAsync(Alarm alarm)
        {
            try
            {
                _logger.LogDebug("Updating alarm with ID {AlarmId}", alarm.Id);

                _context.Alarms.Update(alarm);
                await _context.SaveChangesAsync();

                return alarm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alarm with ID {AlarmId}", alarm.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete an alarm record
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogDebug("Deleting alarm with ID {AlarmId}", id);

                var alarm = await _context.Alarms.FindAsync(id);
                if (alarm == null)
                {
                    _logger.LogWarning("Alarm with ID {AlarmId} not found for deletion", id);
                    return false;
                }

                _context.Alarms.Remove(alarm);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted alarm with ID {AlarmId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alarm with ID {AlarmId}", id);
                throw;
            }
        }

        /// <summary>
        /// Count total alarms for statistics
        /// Pure data access - no business logic
        /// </summary>
        public async Task<int> CountAsync(int? siteId = null, int days = 30)
        {
            try
            {
                _logger.LogDebug("Counting alarms for site {SiteId} in last {Days} days", siteId, days);

                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var query = _context.Alarms.Where(a => a.Timestamp >= cutoffDate);

                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting alarms for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Count alarms by day for trend analysis
        /// Pure data access - returns raw counts
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetAlarmCountsByDayAsync(int? siteId = null, int days = 7)
        {
            try
            {
                _logger.LogDebug("Getting alarm counts by day for site {SiteId} for last {Days} days", siteId, days);

                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var query = _context.Alarms.Where(a => a.Timestamp >= cutoffDate);

                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                return await query
                    .GroupBy(a => a.Timestamp.Date)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm counts by day for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get alarm message frequency for a site
        /// Pure data access - returns raw frequency data
        /// </summary>
        public async Task<Dictionary<string, int>> GetAlarmTypeFrequencyAsync(int siteId, int days = 30)
        {
            try
            {
                _logger.LogDebug("Getting alarm type frequency for site {SiteId} for last {Days} days", siteId, days);

                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                return await _context.Alarms
                    .Where(a => a.SiteId == siteId && a.Timestamp >= cutoffDate)
                    .GroupBy(a => a.AlarmMessage)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm type frequency for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get alarm statistics for dashboard
        /// Pure data access - returns raw statistical data
        /// </summary>
        public async Task<object> GetAlarmStatisticsAsync(int? siteId = null)
        {
            try
            {
                _logger.LogDebug("Getting alarm statistics for site {SiteId}", siteId);

                var query = _context.Alarms.AsQueryable();

                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                // Get basic statistics
                var totalAlarms = await query.CountAsync();
                var last24Hours = await query.Where(a => a.Timestamp >= DateTime.UtcNow.AddHours(-24)).CountAsync();
                var last7Days = await query.Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-7)).CountAsync();
                var last30Days = await query.Where(a => a.Timestamp >= DateTime.UtcNow.AddDays(-30)).CountAsync();

                return new
                {
                    totalAlarms,
                    last24Hours,
                    last7Days,
                    last30Days,
                    oldestAlarm = await query.MinAsync(a => (DateTime?)a.Timestamp),
                    newestAlarm = await query.MaxAsync(a => (DateTime?)a.Timestamp)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm statistics for site {SiteId}", siteId);
                throw;
            }
        }
    }
}