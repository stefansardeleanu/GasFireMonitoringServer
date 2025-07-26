// File: Controllers/SensorController.cs
// REST API controller for sensor data

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing sensor data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SensorController> _logger;

        public SensorController(ApplicationDbContext context, ILogger<SensorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors for a specific site
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <returns>List of sensors</returns>
        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetSensorsBySite(int siteId)
        {
            try
            {
                var sensors = await _context.Sensors
                    .Where(s => s.SiteId == siteId)
                    .OrderBy(s => s.ChannelId)
                    .Select(s => new
                    {
                        s.Id,
                        s.SiteId,
                        s.SiteName,
                        s.ChannelId,
                        s.TagName,
                        s.DetectorType,
                        s.ProcessValue,
                        s.CurrentValue,
                        s.Status,
                        s.StatusText,
                        s.Units,
                        s.LastUpdated
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = sensors.Count,
                    data = sensors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sensors for site {siteId}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving sensors" });
            }
        }

        /// <summary>
        /// Get all sensors across all sites
        /// </summary>
        /// <returns>List of all sensors grouped by site</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllSensors()
        {
            try
            {
                var sensors = await _context.Sensors
                    .GroupBy(s => new { s.SiteId, s.SiteName })
                    .Select(g => new
                    {
                        siteId = g.Key.SiteId,
                        siteName = g.Key.SiteName,
                        sensorCount = g.Count(),
                        sensors = g.Select(s => new
                        {
                            s.Id,
                            s.ChannelId,
                            s.TagName,
                            s.DetectorType,
                            s.ProcessValue,
                            s.CurrentValue,
                            s.Status,
                            s.StatusText,
                            s.Units,
                            s.LastUpdated
                        }).OrderBy(s => s.ChannelId).ToList()
                    })
                    .OrderBy(g => g.siteId)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    siteCount = sensors.Count,
                    data = sensors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sensors");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving sensors" });
            }
        }

        /// <summary>
        /// Get a specific sensor by ID
        /// </summary>
        /// <param name="id">Sensor ID</param>
        /// <returns>Sensor details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSensor(int id)
        {
            try
            {
                var sensor = await _context.Sensors
                    .Where(s => s.Id == id)
                    .Select(s => new
                    {
                        s.Id,
                        s.SiteId,
                        s.SiteName,
                        s.ChannelId,
                        s.TagName,
                        s.DetectorType,
                        s.ProcessValue,
                        s.CurrentValue,
                        s.Status,
                        s.StatusText,
                        s.Units,
                        s.LastUpdated,
                        s.Topic
                    })
                    .FirstOrDefaultAsync();

                if (sensor == null)
                {
                    return NotFound(new { success = false, message = "Sensor not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = sensor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sensor {id}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the sensor" });
            }
        }

        /// <summary>
        /// Get sensors in alarm state
        /// </summary>
        /// <returns>List of sensors currently in alarm</returns>
        [HttpGet("alarms")]
        public async Task<IActionResult> GetSensorsInAlarm()
        {
            try
            {
                var alarmedSensors = await _context.Sensors
                    .Where(s => s.Status > 0) // Status > 0 means some kind of alarm
                    .OrderBy(s => s.SiteId)
                    .ThenBy(s => s.ChannelId)
                    .Select(s => new
                    {
                        s.Id,
                        s.SiteId,
                        s.SiteName,
                        s.ChannelId,
                        s.TagName,
                        s.DetectorType,
                        s.ProcessValue,
                        s.CurrentValue,
                        s.Status,
                        s.StatusText,
                        s.Units,
                        s.LastUpdated
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = alarmedSensors.Count,
                    data = alarmedSensors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors in alarm");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving alarm sensors" });
            }
        }

        /// <summary>
        /// Get sensor statistics for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Statistics about sensors at the site</returns>
        [HttpGet("site/{siteId}/stats")]
        public async Task<IActionResult> GetSiteStatistics(int siteId)
        {
            try
            {
                var sensors = await _context.Sensors
                    .Where(s => s.SiteId == siteId)
                    .ToListAsync();

                var stats = new
                {
                    totalSensors = sensors.Count,
                    normalSensors = sensors.Count(s => s.Status == 0),
                    alarmLevel1 = sensors.Count(s => s.Status == 1),
                    alarmLevel2 = sensors.Count(s => s.Status == 2),
                    errorSensors = sensors.Count(s => s.Status >= 3),
                    sensorTypes = sensors.GroupBy(s => s.DetectorType)
                        .Select(g => new
                        {
                            type = g.Key,
                            typeName = GetDetectorTypeName(g.Key),
                            count = g.Count()
                        })
                        .ToList()
                };

                return Ok(new
                {
                    success = true,
                    siteId = siteId,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving statistics for site {siteId}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving statistics" });
            }
        }

        // Helper method to get detector type name
        private string GetDetectorTypeName(int type)
        {
            return type switch
            {
                1 => "Gas",
                2 => "Flame",
                3 => "Manual Call",
                4 => "Smoke",
                _ => "Unknown"
            };
        }
    }
}