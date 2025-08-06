// File: Repositories/SensorRepository.cs
// Concrete implementation of ISensorRepository
// Handles all sensor database operations

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Repositories
{
    /// <summary>
    /// Repository implementation for sensor data operations
    /// Pure data access layer - no business logic
    /// </summary>
    public class SensorRepository : ISensorRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SensorRepository> _logger;

        public SensorRepository(ApplicationDbContext context, ILogger<SensorRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors from all sites
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all sensors from database");

                return await _context.Sensors
                    .OrderBy(s => s.SiteId)
                    .ThenBy(s => s.ChannelId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sensors from database");
                throw;
            }
        }

        /// <summary>
        /// Get all sensors for a specific site
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetBySiteIdAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Retrieving sensors for site {SiteId}", siteId);

                return await _context.Sensors
                    .Where(s => s.SiteId == siteId)
                    .OrderBy(s => s.ChannelId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get a specific sensor by its ID
        /// </summary>
        public async Task<Sensor?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving sensor with ID {SensorId}", id);

                return await _context.Sensors
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensor with ID {SensorId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get all sensors currently in alarm state (Status = 1 or 2)
        /// </summary>
        public async Task<IEnumerable<Sensor>> GetSensorsInAlarmAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving sensors in alarm state (status 1 or 2)");

                return await _context.Sensors
                    .Where(s => s.Status == 1 || s.Status == 2) // Only actual alarms, not faults/disabled
                    .OrderBy(s => s.SiteId)
                    .ThenBy(s => s.ChannelId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors in alarm");
                throw;
            }
        }

        /// <summary>
        /// Create a new sensor record
        /// </summary>
        public async Task<Sensor> CreateAsync(Sensor sensor)
        {
            try
            {
                _logger.LogDebug("Creating new sensor for site {SiteId}, channel {ChannelId}",
                    sensor.SiteId, sensor.ChannelId);

                _context.Sensors.Add(sensor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created sensor with ID {SensorId}", sensor.Id);
                return sensor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sensor for site {SiteId}, channel {ChannelId}",
                    sensor.SiteId, sensor.ChannelId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing sensor record
        /// </summary>
        public async Task<Sensor> UpdateAsync(Sensor sensor)
        {
            try
            {
                _logger.LogDebug("Updating sensor with ID {SensorId}", sensor.Id);

                _context.Sensors.Update(sensor);
                await _context.SaveChangesAsync();

                return sensor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor with ID {SensorId}", sensor.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete a sensor record
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogDebug("Deleting sensor with ID {SensorId}", id);

                var sensor = await _context.Sensors.FindAsync(id);
                if (sensor == null)
                {
                    _logger.LogWarning("Sensor with ID {SensorId} not found for deletion", id);
                    return false;
                }

                _context.Sensors.Remove(sensor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted sensor with ID {SensorId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sensor with ID {SensorId}", id);
                throw;
            }
        }

        /// <summary>
        /// Count total sensors for a specific site
        /// </summary>
        public async Task<int> CountBySiteIdAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Counting sensors for site {SiteId}", siteId);

                return await _context.Sensors
                    .CountAsync(s => s.SiteId == siteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting sensors for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Count sensors by status for a specific site
        /// Pure data access - no business logic
        /// </summary>
        public async Task<int> CountByStatusAsync(int siteId, int status)
        {
            try
            {
                _logger.LogDebug("Counting sensors with status {Status} for site {SiteId}", status, siteId);

                return await _context.Sensors
                    .CountAsync(s => s.SiteId == siteId && s.Status == status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting sensors with status {Status} for site {SiteId}", status, siteId);
                throw;
            }
        }

        /// <summary>
        /// Get the last update time for all sensors at a site
        /// Pure data access - used by service layer to determine connectivity
        /// </summary>
        public async Task<DateTime?> GetLastUpdateTimeAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting last update time for site {SiteId}", siteId);

                return await _context.Sensors
                    .Where(s => s.SiteId == siteId)
                    .MaxAsync(s => (DateTime?)s.LastUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last update time for site {SiteId}", siteId);
                throw;
            }
        }
    }
}