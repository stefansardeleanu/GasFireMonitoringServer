// File: Controllers/AlarmController.cs
// REST API controller for alarm data

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing alarm data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AlarmController> _logger;

        public AlarmController(ApplicationDbContext context, ILogger<AlarmController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get alarms with optional filtering
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <param name="limit">Maximum number of records to return (default 100)</param>
        /// <returns>List of alarms</returns>
        [HttpGet]
        public async Task<IActionResult> GetAlarms(
            [FromQuery] int? siteId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var query = _context.Alarms.AsQueryable();

                // Apply filters
                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                // Get alarms ordered by timestamp descending (newest first)
                var alarms = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.SiteId,
                        a.SiteName,
                        a.SensorTag,
                        a.AlarmMessage,
                        a.RawMessage,
                        a.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = alarms.Count,
                    data = alarms
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving alarms" });
            }
        }

        /// <summary>
        /// Get alarms for a specific site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="limit">Maximum number of records to return (default 50)</param>
        /// <returns>List of alarms for the site</returns>
        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetAlarmsBySite(int siteId, [FromQuery] int limit = 50)
        {
            try
            {
                var alarms = await _context.Alarms
                    .Where(a => a.SiteId == siteId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.SensorTag,
                        a.AlarmMessage,
                        a.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    siteId = siteId,
                    count = alarms.Count,
                    data = alarms
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving alarms for site {siteId}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving alarms" });
            }
        }

        /// <summary>
        /// Get alarm statistics
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="days">Number of days to include (default 7)</param>
        /// <returns>Alarm statistics</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetAlarmStatistics([FromQuery] int? siteId = null, [FromQuery] int days = 7)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var query = _context.Alarms.Where(a => a.Timestamp >= startDate);

                if (siteId.HasValue)
                {
                    query = query.Where(a => a.SiteId == siteId.Value);
                }

                var alarms = await query.ToListAsync();

                var stats = new
                {
                    totalAlarms = alarms.Count,
                    alarmsBySite = alarms.GroupBy(a => new { a.SiteId, a.SiteName })
                        .Select(g => new
                        {
                            siteId = g.Key.SiteId,
                            siteName = g.Key.SiteName,
                            count = g.Count()
                        })
                        .OrderByDescending(x => x.count)
                        .ToList(),
                    alarmsByDay = alarms.GroupBy(a => a.Timestamp.Date)
                        .Select(g => new
                        {
                            date = g.Key.ToString("yyyy-MM-dd"),
                            count = g.Count()
                        })
                        .OrderBy(x => x.date)
                        .ToList(),
                    topSensors = alarms.GroupBy(a => a.SensorTag)
                        .Select(g => new
                        {
                            sensorTag = g.Key,
                            count = g.Count()
                        })
                        .OrderByDescending(x => x.count)
                        .Take(10)
                        .ToList()
                };

                return Ok(new
                {
                    success = true,
                    periodDays = days,
                    startDate = startDate,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarm statistics");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving statistics" });
            }
        }

        /// <summary>
        /// Get active alarms (latest alarm for each sensor that hasn't returned to normal)
        /// </summary>
        /// <returns>List of active alarms</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAlarms()
        {
            try
            {
                // Get sensors that are currently in alarm state
                var alarmedSensors = await _context.Sensors
                    .Where(s => s.Status > 0)
                    .ToListAsync();

                // Get the latest alarm for each alarmed sensor
                var activeAlarms = new List<object>();

                foreach (var sensor in alarmedSensors)
                {
                    var latestAlarm = await _context.Alarms
                        .Where(a => a.SiteId == sensor.SiteId && a.SensorTag == sensor.TagName)
                        .OrderByDescending(a => a.Timestamp)
                        .FirstOrDefaultAsync();

                    if (latestAlarm != null)
                    {
                        activeAlarms.Add(new
                        {
                            sensorId = sensor.Id,
                            sensor.SiteId,
                            sensor.SiteName,
                            sensor.TagName,
                            sensor.ChannelId,
                            currentStatus = sensor.Status,
                            currentStatusText = sensor.StatusText,
                            currentValue = sensor.ProcessValue,
                            sensor.Units,
                            alarmId = latestAlarm.Id,
                            alarmMessage = latestAlarm.AlarmMessage,
                            alarmTimestamp = latestAlarm.Timestamp,
                            sensor.LastUpdated
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    count = activeAlarms.Count,
                    data = activeAlarms.OrderBy(a => ((dynamic)a).SiteId).ThenBy(a => ((dynamic)a).TagName)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alarms");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving active alarms" });
            }
        }

        /// <summary>
        /// Get alarm history for a specific sensor
        /// </summary>
        /// <param name="sensorTag">Sensor tag name</param>
        /// <param name="limit">Maximum number of records (default 50)</param>
        /// <returns>List of alarms for the sensor</returns>
        [HttpGet("sensor/{sensorTag}")]
        public async Task<IActionResult> GetAlarmsBySensor(string sensorTag, [FromQuery] int limit = 50)
        {
            try
            {
                var alarms = await _context.Alarms
                    .Where(a => a.SensorTag == sensorTag)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.SiteId,
                        a.SiteName,
                        a.AlarmMessage,
                        a.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    sensorTag = sensorTag,
                    count = alarms.Count,
                    data = alarms
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving alarms for sensor {sensorTag}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving alarms" });
            }
        }

        /// <summary>
        /// Delete old alarms (cleanup)
        /// </summary>
        /// <param name="daysToKeep">Number of days of history to keep (default 30)</param>
        /// <returns>Number of alarms deleted</returns>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldAlarms([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var alarmsToDelete = await _context.Alarms
                    .Where(a => a.Timestamp < cutoffDate)
                    .ToListAsync();

                _context.Alarms.RemoveRange(alarmsToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted {alarmsToDelete.Count} alarms older than {cutoffDate}");

                return Ok(new
                {
                    success = true,
                    deletedCount = alarmsToDelete.Count,
                    cutoffDate = cutoffDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old alarms");
                return StatusCode(500, new { success = false, message = "An error occurred while cleaning up alarms" });
            }
        }
    }
}