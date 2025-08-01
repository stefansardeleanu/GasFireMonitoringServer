// File: Controllers/SiteController.cs
// REST API controller for site information

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GasFireMonitoringServer.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing site information
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SiteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SiteController> _logger;

        // Site information (hardcoded for now, could be moved to database)
        private readonly List<SiteInfo> _sites = new()
        {
            new SiteInfo { Id = 1, Name = "SondaMorEni", County = "Prahova", Latitude = 40, Longitude = 10 },
            new SiteInfo { Id = 2, Name = "SondaArb", County = "Prahova", Latitude = 40, Longitude = 20 },
            new SiteInfo { Id = 3, Name = "SondaBerPH01", County = "Prahova", Latitude = 40, Longitude = 30 },
            new SiteInfo { Id = 4, Name = "ParcMorMic", County = "Prahova", Latitude = 40, Longitude = 40 },
            new SiteInfo { Id = 5, Name = "PanouHurezani", County = "Gorj", Latitude = 55, Longitude = 75 },
            new SiteInfo { Id = 6, Name = "TUCOBulbuceni", County = "Gorj", Latitude = 65, Longitude = 65 },
            new SiteInfo { Id = 7, Name = "ParcBatrani", County = "Prahova", Latitude = 40, Longitude = 50 },
            new SiteInfo { Id = 8, Name = "ParcCartojani", County = "Prahova", Latitude = 40, Longitude = 60 },
            new SiteInfo { Id = 9, Name = "ParcTintea", County = "Prahova", Latitude = 40, Longitude = 70 },
            new SiteInfo { Id = 10, Name = "StatieLucacesti", County = "Prahova", Latitude = 40, Longitude = 807 }
        };

        public SiteController(ApplicationDbContext context, ILogger<SiteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all sites with their current status
        /// </summary>
        /// <returns>List of all sites with sensor counts and alarm status</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllSites()
        {
            try
            {
                // Get sensor data from database
                var sensorData = await _context.Sensors
                    .GroupBy(s => s.SiteId)
                    .Select(g => new
                    {
                        SiteId = g.Key,
                        TotalSensors = g.Count(),
                        NormalSensors = g.Count(s => s.Status == 0),
                        AlarmSensors = g.Count(s => s.Status > 0 && s.Status <= 2),
                        ErrorSensors = g.Count(s => s.Status > 2),
                        LastUpdate = g.Max(s => s.LastUpdated)
                    })
                    .ToListAsync();

                // Combine with site information
                var sites = _sites.Select(site =>
                {
                    var data = sensorData.FirstOrDefault(d => d.SiteId == site.Id);
                    return new
                    {
                        site.Id,
                        site.Name,
                        site.County,
                        site.Latitude,
                        site.Longitude,
                        status = GetSiteStatus(data),
                        totalSensors = data?.TotalSensors ?? 0,
                        normalSensors = data?.NormalSensors ?? 0,
                        alarmSensors = data?.AlarmSensors ?? 0,
                        errorSensors = data?.ErrorSensors ?? 0,
                        lastUpdate = data?.LastUpdate ?? DateTime.MinValue
                    };
                }).OrderBy(s => s.County).ThenBy(s => s.Name).ToList();

                return Ok(new
                {
                    success = true,
                    count = sites.Count,
                    data = sites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving sites" });
            }
        }

        /// <summary>
        /// Get sites grouped by county
        /// </summary>
        /// <returns>Sites grouped by county with status</returns>
        [HttpGet("by-county")]
        public async Task<IActionResult> GetSitesByCounty()
        {
            try
            {
                // Get sensor data
                var sensorData = await _context.Sensors
                    .GroupBy(s => s.SiteId)
                    .Select(g => new
                    {
                        SiteId = g.Key,
                        HasAlarms = g.Any(s => s.Status > 0 && s.Status <= 2),
                        HasErrors = g.Any(s => s.Status > 2)
                    })
                    .ToListAsync();

                // Group sites by county
                var counties = _sites.GroupBy(s => s.County)
                    .Select(county => new
                    {
                        county = county.Key,
                        siteCount = county.Count(),
                        sites = county.Select(site =>
                        {
                            var data = sensorData.FirstOrDefault(d => d.SiteId == site.Id);
                            return new
                            {
                                site.Id,
                                site.Name,
                                site.Latitude,
                                site.Longitude,
                                hasAlarms = data?.HasAlarms ?? false,
                                hasErrors = data?.HasErrors ?? false
                            };
                        }).OrderBy(s => s.Name).ToList()
                    })
                    .OrderBy(c => c.county)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    countyCount = counties.Count,
                    data = counties
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites by county");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving sites" });
            }
        }

        /// <summary>
        /// Get detailed information for a specific site
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>Detailed site information including sensors</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSite(int id)
        {
            try
            {
                var site = _sites.FirstOrDefault(s => s.Id == id);
                if (site == null)
                {
                    return NotFound(new { success = false, message = "Site not found" });
                }

                // Get sensors for this site
                var sensors = await _context.Sensors
                    .Where(s => s.SiteId == id)
                    .OrderBy(s => s.ChannelId)
                    .Select(s => new
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
                    })
                    .ToListAsync();

                // Get recent alarms for this site
                var recentAlarms = await _context.Alarms
                    .Where(a => a.SiteId == id)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .Select(a => new
                    {
                        a.Id,
                        a.SensorTag,
                        a.AlarmMessage,
                        a.Timestamp
                    })
                    .ToListAsync();

                var siteDetails = new
                {
                    site.Id,
                    site.Name,
                    site.County,
                    site.Latitude,
                    site.Longitude,
                    status = GetSiteStatusFromSensors(sensors),
                    statistics = new
                    {
                        totalSensors = sensors.Count,
                        normalSensors = sensors.Count(s => s.Status == 0),
                        alarmSensors = sensors.Count(s => s.Status > 0 && s.Status <= 2),
                        errorSensors = sensors.Count(s => s.Status > 2),
                        sensorTypes = sensors.GroupBy(s => s.DetectorType)
                            .Select(g => new
                            {
                                type = g.Key,
                                typeName = GetDetectorTypeName(g.Key),
                                count = g.Count()
                            })
                            .ToList()
                    },
                    sensors = sensors,
                    recentAlarms = recentAlarms
                };

                return Ok(new
                {
                    success = true,
                    data = siteDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving site {id}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving site details" });
            }
        }

        /// <summary>
        /// Get site status summary
        /// </summary>
        /// <returns>Summary of all sites status</returns>
        [HttpGet("status-summary")]
        public async Task<IActionResult> GetStatusSummary()
        {
            try
            {
                var sensorData = await _context.Sensors
                    .GroupBy(s => s.SiteId)
                    .Select(g => new
                    {
                        SiteId = g.Key,
                        HasAlarms = g.Any(s => s.Status > 0 && s.Status <= 2),
                        HasErrors = g.Any(s => s.Status > 2),
                        IsOffline = g.All(s => s.LastUpdated < DateTime.UtcNow.AddMinutes(-30))
                    })
                    .ToListAsync();

                var summary = new
                {
                    totalSites = _sites.Count,
                    sitesNormal = sensorData.Count(s => !s.HasAlarms && !s.HasErrors && !s.IsOffline),
                    sitesWithAlarms = sensorData.Count(s => s.HasAlarms),
                    sitesWithErrors = sensorData.Count(s => s.HasErrors),
                    sitesOffline = sensorData.Count(s => s.IsOffline),
                    timestamp = DateTime.UtcNow
                };

                return Ok(new
                {
                    success = true,
                    data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status summary");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving status summary" });
            }
        }

        // Helper methods
        private string GetSiteStatus(dynamic data)
        {
            if (data == null) return "offline";
            if (data.AlarmSensors > 0) return "alarm";
            if (data.ErrorSensors > 0) return "error";
            if (data.LastUpdate < DateTime.UtcNow.AddMinutes(-30)) return "offline";
            return "normal";
        }

        private string GetSiteStatusFromSensors(dynamic sensors)
        {
            if (sensors == null || sensors.Count == 0) return "offline";

            // Check for alarms (status 1 or 2)
            bool hasAlarms = false;
            bool hasErrors = false;
            DateTime? lastUpdate = null;

            foreach (var sensor in sensors)
            {
                if (sensor.Status > 0 && sensor.Status <= 2) hasAlarms = true;
                if (sensor.Status > 2) hasErrors = true;
                if (lastUpdate == null || sensor.LastUpdated > lastUpdate)
                    lastUpdate = sensor.LastUpdated;
            }

            if (hasAlarms) return "alarm";
            if (hasErrors) return "error";
            if (lastUpdate.HasValue && lastUpdate.Value < DateTime.UtcNow.AddMinutes(-30)) return "offline";
            return "normal";
        }

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

        // Inner class for site information
        private class SiteInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string County { get; set; } = "";
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}