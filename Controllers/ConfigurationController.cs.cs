// File: Controllers/ConfigurationController.cs
// CORRECTED Configuration API endpoints that match your existing SiteConfigurationModel structure
// Includes LayoutMode, GridConfig, and all properties your data has

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// Configuration management API controller
    /// Handles your existing site structure with LayoutMode and GridConfig
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationService configurationService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all site configurations with full structure
        /// </summary>
        [HttpGet("sites")]
        public async Task<ActionResult<IEnumerable<SiteConfigResponse>>> GetAllSiteConfigurations()
        {
            try
            {
                _logger.LogInformation("Getting all site configurations");

                var sites = await _configurationService.GetAllSitesAsync();
                var response = sites.Select(site => new SiteConfigResponse
                {
                    Id = site.Id,
                    Name = site.Name,
                    County = site.County,
                    MapX = site.MapX,
                    MapY = site.MapY,
                    LayoutMode = site.LayoutMode,
                    LayoutFile = site.LayoutFile,
                    HasCustomLayout = !string.IsNullOrEmpty(site.LayoutFile),
                    GridConfig = new GridConfigResponse
                    {
                        Columns = site.GridConfig.Columns,
                        Style = site.GridConfig.Style,
                        Spacing = site.GridConfig.Spacing,
                        GroupByType = site.GridConfig.GroupByType,
                        ShowLabels = site.GridConfig.ShowLabels
                    }
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site configurations");
                return StatusCode(500, new { error = "Failed to get site configurations" });
            }
        }

        /// <summary>
        /// Get specific site configuration
        /// </summary>
        [HttpGet("sites/{id:int}")]
        public async Task<ActionResult<SiteConfigResponse>> GetSiteConfiguration(int id)
        {
            try
            {
                _logger.LogInformation("Getting site configuration for ID {SiteId}", id);

                var site = await _configurationService.GetSiteAsync(id);
                if (site == null)
                {
                    return NotFound(new { error = $"Site {id} not found" });
                }

                var response = new SiteConfigResponse
                {
                    Id = site.Id,
                    Name = site.Name,
                    County = site.County,
                    MapX = site.MapX,
                    MapY = site.MapY,
                    LayoutMode = site.LayoutMode,
                    LayoutFile = site.LayoutFile,
                    HasCustomLayout = !string.IsNullOrEmpty(site.LayoutFile),
                    GridConfig = new GridConfigResponse
                    {
                        Columns = site.GridConfig.Columns,
                        Style = site.GridConfig.Style,
                        Spacing = site.GridConfig.Spacing,
                        GroupByType = site.GridConfig.GroupByType,
                        ShowLabels = site.GridConfig.ShowLabels
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site configuration for {SiteId}", id);
                return StatusCode(500, new { error = "Failed to get site configuration" });
            }
        }

        /// <summary>
        /// Update site configuration
        /// </summary>
        [HttpPut("sites/{id:int}")]
        public async Task<ActionResult<SiteConfigResponse>> UpdateSiteConfiguration(int id, [FromBody] SiteUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating site configuration for ID {SiteId}", id);

                // Get existing site
                var existingSite = await _configurationService.GetSiteAsync(id);
                if (existingSite == null)
                {
                    return NotFound(new { error = $"Site {id} not found" });
                }

                // Update the site properties
                existingSite.Name = request.Name;
                existingSite.County = request.County;
                existingSite.MapX = request.MapX;
                existingSite.MapY = request.MapY;
                existingSite.LayoutMode = request.LayoutMode;
                existingSite.LayoutFile = request.LayoutFile ?? "";

                // Update grid config if provided
                if (request.GridConfig != null)
                {
                    existingSite.GridConfig.Columns = request.GridConfig.Columns;
                    existingSite.GridConfig.Style = request.GridConfig.Style;
                    existingSite.GridConfig.Spacing = request.GridConfig.Spacing;
                    existingSite.GridConfig.GroupByType = request.GridConfig.GroupByType;
                    existingSite.GridConfig.ShowLabels = request.GridConfig.ShowLabels;
                }

                // Save using existing method
                var success = await _configurationService.SaveSiteAsync(existingSite);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to save site configuration" });
                }

                var response = new SiteConfigResponse
                {
                    Id = existingSite.Id,
                    Name = existingSite.Name,
                    County = existingSite.County,
                    MapX = existingSite.MapX,
                    MapY = existingSite.MapY,
                    LayoutMode = existingSite.LayoutMode,
                    LayoutFile = existingSite.LayoutFile,
                    HasCustomLayout = !string.IsNullOrEmpty(existingSite.LayoutFile),
                    GridConfig = new GridConfigResponse
                    {
                        Columns = existingSite.GridConfig.Columns,
                        Style = existingSite.GridConfig.Style,
                        Spacing = existingSite.GridConfig.Spacing,
                        GroupByType = existingSite.GridConfig.GroupByType,
                        ShowLabels = existingSite.GridConfig.ShowLabels
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site configuration for {SiteId}", id);
                return StatusCode(500, new { error = "Failed to update site configuration" });
            }
        }

        /// <summary>
        /// Create new site configuration
        /// </summary>
        [HttpPost("sites")]
        public async Task<ActionResult<SiteConfigResponse>> CreateSiteConfiguration([FromBody] SiteCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating site configuration for {SiteName}", request.Name);

                // Check if site already exists
                var existingSite = await _configurationService.GetSiteAsync(request.Id);
                if (existingSite != null)
                {
                    return Conflict(new { error = $"Site with ID {request.Id} already exists" });
                }

                // Create new site
                var newSite = new SiteConfigurationModel
                {
                    Id = request.Id,
                    Name = request.Name,
                    County = request.County,
                    MapX = request.MapX,
                    MapY = request.MapY,
                    LayoutMode = request.LayoutMode,
                    LayoutFile = request.LayoutFile ?? "",
                    GridConfig = new GridLayoutConfig
                    {
                        Columns = request.GridConfig?.Columns ?? 0,
                        Style = request.GridConfig?.Style ?? "square",
                        Spacing = request.GridConfig?.Spacing ?? 10.0,
                        GroupByType = request.GridConfig?.GroupByType ?? true,
                        ShowLabels = request.GridConfig?.ShowLabels ?? true
                    }
                };

                // Save using existing method
                var success = await _configurationService.SaveSiteAsync(newSite);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to save site configuration" });
                }

                var response = new SiteConfigResponse
                {
                    Id = newSite.Id,
                    Name = newSite.Name,
                    County = newSite.County,
                    MapX = newSite.MapX,
                    MapY = newSite.MapY,
                    LayoutMode = newSite.LayoutMode,
                    LayoutFile = newSite.LayoutFile,
                    HasCustomLayout = !string.IsNullOrEmpty(newSite.LayoutFile),
                    GridConfig = new GridConfigResponse
                    {
                        Columns = newSite.GridConfig.Columns,
                        Style = newSite.GridConfig.Style,
                        Spacing = newSite.GridConfig.Spacing,
                        GroupByType = newSite.GridConfig.GroupByType,
                        ShowLabels = newSite.GridConfig.ShowLabels
                    }
                };

                return CreatedAtAction(nameof(GetSiteConfiguration), new { id = newSite.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site configuration");
                return StatusCode(500, new { error = "Failed to create site configuration" });
            }
        }

        /// <summary>
        /// Delete site configuration
        /// </summary>
        [HttpDelete("sites/{id:int}")]
        public async Task<ActionResult> DeleteSiteConfiguration(int id)
        {
            try
            {
                _logger.LogInformation("Deleting site configuration for ID {SiteId}", id);

                var success = await _configurationService.DeleteSiteAsync(id);
                if (!success)
                {
                    return NotFound(new { error = $"Site {id} not found" });
                }

                return Ok(new { message = $"Site {id} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site configuration for {SiteId}", id);
                return StatusCode(500, new { error = "Failed to delete site configuration" });
            }
        }

        /// <summary>
        /// Get sensor configurations for a site
        /// </summary>
        [HttpGet("sensors/site/{siteId:int}")]
        public async Task<ActionResult<IEnumerable<SensorConfigDto>>> GetSensorConfigurations(int siteId)
        {
            try
            {
                _logger.LogInformation("Getting sensor configurations for site {SiteId}", siteId);

                var sensors = await _configurationService.GetSensorsBySiteAsync(siteId);
                var response = sensors.Select(sensor => new SensorConfigDto
                {
                    SiteId = sensor.SiteId,
                    ChannelId = sensor.ChannelId,
                    LayoutX = sensor.LayoutX,
                    LayoutY = sensor.LayoutY,
                    DisplayName = sensor.DisplayName
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor configurations for site {SiteId}", siteId);
                return StatusCode(500, new { error = "Failed to get sensor configurations" });
            }
        }

        /// <summary>
        /// Update sensor positions for a site
        /// </summary>
        [HttpPut("sensors/site/{siteId:int}")]
        public async Task<ActionResult<IEnumerable<SensorConfigDto>>> UpdateSensorConfigurations(int siteId, [FromBody] SensorUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating sensor configurations for site {SiteId}", siteId);

                var allSensors = (await _configurationService.GetAllSensorsAsync()).ToList();
                var siteSensors = allSensors.Where(s => s.SiteId == siteId).ToList();

                if (!siteSensors.Any())
                {
                    return NotFound(new { error = $"No sensors found for site {siteId}" });
                }

                var updatedSensors = new List<SensorConfigurationModel>();

                foreach (var update in request.Sensors)
                {
                    var sensor = siteSensors.FirstOrDefault(s => s.ChannelId == update.ChannelId);
                    if (sensor != null)
                    {
                        sensor.LayoutX = update.LayoutX;
                        sensor.LayoutY = update.LayoutY;
                        updatedSensors.Add(sensor);

                        // Save individual sensor using existing method
                        await _configurationService.SaveSensorAsync(sensor);
                    }
                }

                var response = updatedSensors.Select(sensor => new SensorConfigDto
                {
                    SiteId = sensor.SiteId,
                    ChannelId = sensor.ChannelId,
                    LayoutX = sensor.LayoutX,
                    LayoutY = sensor.LayoutY,
                    DisplayName = sensor.DisplayName
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor configurations for site {SiteId}", siteId);
                return StatusCode(500, new { error = "Failed to update sensor configurations" });
            }
        }

        /// <summary>
        /// Get all counties
        /// </summary>
        [HttpGet("counties")]
        public async Task<ActionResult<IEnumerable<CountyConfigDto>>> GetCountyConfigurations()
        {
            try
            {
                _logger.LogInformation("Getting county configurations");

                var counties = await _configurationService.GetAllCountiesAsync();
                var response = counties.Select(county => new CountyConfigDto
                {
                    Name = county.Name,
                    SiteIds = county.Sites.ToList(),
                    SiteCount = county.Sites.Count()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting county configurations");
                return StatusCode(500, new { error = "Failed to get county configurations" });
            }
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        [HttpGet("validate")]
        public async Task<ActionResult<ConfigValidationDto>> ValidateConfiguration()
        {
            try
            {
                _logger.LogInformation("Validating configuration");

                var result = await _configurationService.ValidateConfigurationAsync();
                var response = new ConfigValidationDto
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors,
                    Warnings = result.Warnings,
                    SiteCount = result.SiteCount,
                    SensorCount = result.SensorCount,
                    CountyCount = result.CountyCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                return StatusCode(500, new { error = "Failed to validate configuration" });
            }
        }

        /// <summary>
        /// Debug endpoint to troubleshoot configuration loading
        /// </summary>
        [HttpGet("debug")]
        public async Task<ActionResult> DebugConfiguration()
        {
            try
            {
                var paths = _configurationService.GetConfigurationPaths();

                return Ok(new
                {
                    Paths = new
                    {
                        SitesPath = paths.SitesPath,
                        SensorsPath = paths.SensorsPath,
                        CountiesPath = paths.CountiesPath
                    },
                    FileExists = new
                    {
                        SitesExists = System.IO.File.Exists(paths.SitesPath),
                        SensorsExists = System.IO.File.Exists(paths.SensorsPath),
                        CountiesExists = System.IO.File.Exists(paths.CountiesPath)
                    },
                    SitesFileInfo = System.IO.File.Exists(paths.SitesPath) ? new
                    {
                        Size = new System.IO.FileInfo(paths.SitesPath).Length,
                        LastModified = System.IO.File.GetLastWriteTime(paths.SitesPath),
                        FirstChars = (await System.IO.File.ReadAllTextAsync(paths.SitesPath)).Take(200)
                    } : null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}

#region DTOs - Updated for Your Structure

/// <summary>
/// Site configuration response DTO - matches your structure
/// </summary>
public class SiteConfigResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public double MapX { get; set; }
    public double MapY { get; set; }
    public string LayoutMode { get; set; } = "auto";
    public string LayoutFile { get; set; } = string.Empty;
    public bool HasCustomLayout { get; set; }
    public GridConfigResponse GridConfig { get; set; } = new();
}

/// <summary>
/// Grid configuration response DTO
/// </summary>
public class GridConfigResponse
{
    public int Columns { get; set; }
    public string Style { get; set; } = "square";
    public double Spacing { get; set; }
    public bool GroupByType { get; set; }
    public bool ShowLabels { get; set; }
}

/// <summary>
/// Site update request DTO
/// </summary>
public class SiteUpdateRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string County { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public double MapX { get; set; }

    [Required]
    [Range(0, 100)]
    public double MapY { get; set; }

    [Required]
    public string LayoutMode { get; set; } = "auto";

    public string? LayoutFile { get; set; }

    public GridConfigRequest? GridConfig { get; set; }
}

/// <summary>
/// Site create request DTO
/// </summary>
public class SiteCreateRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string County { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public double MapX { get; set; }

    [Required]
    [Range(0, 100)]
    public double MapY { get; set; }

    [Required]
    public string LayoutMode { get; set; } = "auto";

    public string? LayoutFile { get; set; }

    public GridConfigRequest? GridConfig { get; set; }
}

/// <summary>
/// Grid configuration request DTO
/// </summary>
public class GridConfigRequest
{
    [Range(0, 20)]
    public int Columns { get; set; } = 0;

    [Required]
    public string Style { get; set; } = "square";

    [Range(0, 50)]
    public double Spacing { get; set; } = 10.0;

    public bool GroupByType { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
}

/// <summary>
/// Sensor configuration response DTO
/// </summary>
public class SensorConfigDto
{
    public int SiteId { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public double LayoutX { get; set; }
    public double LayoutY { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Sensor update request DTO
/// </summary>
public class SensorUpdateRequest
{
    [Required]
    public List<SensorPositionDto> Sensors { get; set; } = new();
}

/// <summary>
/// Individual sensor position DTO
/// </summary>
public class SensorPositionDto
{
    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string ChannelId { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public double LayoutX { get; set; }

    [Required]
    [Range(0, 100)]
    public double LayoutY { get; set; }
}

/// <summary>
/// County configuration response DTO
/// </summary>
public class CountyConfigDto
{
    public string Name { get; set; } = string.Empty;
    public List<int> SiteIds { get; set; } = new();
    public int SiteCount { get; set; }
}

/// <summary>
/// Configuration validation response DTO
/// </summary>
public class ConfigValidationDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int SiteCount { get; set; }
    public int SensorCount { get; set; }
    public int CountyCount { get; set; }
}

#endregion