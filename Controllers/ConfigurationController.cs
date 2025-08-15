// File: Controllers/ConfigurationController.cs
// Configuration management API controller with comprehensive XML documentation

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing system configuration including sites, sensors, and layout settings
    /// </summary>
    /// <remarks>
    /// This controller provides comprehensive configuration management for the gas and fire monitoring
    /// system, handling both operational site settings and visual layout configurations.
    /// All configuration changes are stored in JSON files and can be dynamically updated without system restart.
    /// 
    /// Configuration Management Features:
    /// - Site configuration: Location coordinates, layout modes, grid settings
    /// - Sensor positioning: Layout coordinates for SVG and grid-based displays
    /// - County organization: Administrative grouping of industrial sites
    /// - Configuration validation: Comprehensive validation of all settings
    /// 
    /// Layout System Support:
    /// - SVG Layout Mode: Custom vector graphics with precise sensor positioning
    /// - Grid Layout Mode: Automatic sensor arrangement in configurable grids
    /// - Auto Layout Mode: Intelligent fallback between SVG and grid based on availability
    /// 
    /// Coordinate System:
    /// - Map coordinates (MapX, MapY): 0-100% positioning on county maps
    /// - Layout coordinates (LayoutX, LayoutY): 0-100% positioning on site layouts
    /// - All coordinates are percentage-based for responsive scaling
    /// 
    /// Configuration Files:
    /// - sites.json: Site definitions with layout modes and grid configurations
    /// - sensors.json: Sensor positioning data for custom layouts
    /// - counties.json: Administrative organization of sites by Romanian counties
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigurationController
        /// </summary>
        /// <param name="configurationService">Service for configuration management operations</param>
        /// <param name="logger">Logger for recording configuration operations</param>
        public ConfigurationController(
            IConfigurationService configurationService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all site configurations including layout modes and grid settings
        /// </summary>
        /// <remarks>
        /// Retrieves comprehensive configuration data for all industrial monitoring sites,
        /// including geographic positioning, layout display modes, and grid configuration settings.
        /// This endpoint is essential for client applications that need to understand site
        /// layout capabilities and rendering requirements.
        /// 
        /// Configuration Data Includes:
        /// - Site identification (ID, name, county assignment)
        /// - Geographic positioning (MapX, MapY coordinates for county maps)
        /// - Layout display mode (SVG custom, Grid automatic, or Auto fallback)
        /// - Grid configuration settings (columns, spacing, grouping preferences)
        /// - Custom layout availability status
        /// 
        /// Layout Modes Explained:
        /// - "svg": Uses custom SVG layouts with precise sensor positioning
        /// - "grid": Uses automatic grid-based sensor arrangement
        /// - "auto": Intelligent fallback (tries SVG first, then grid)
        /// 
        /// Use cases:
        /// - Client application initialization and site discovery
        /// - Layout rendering engine configuration
        /// - Administrative configuration management interfaces
        /// - Site deployment planning and coordination
        /// </remarks>
        /// <returns>List of all site configurations with complete layout settings</returns>
        /// <response code="200">Successfully retrieved all site configurations</response>
        /// <response code="500">Internal server error during configuration retrieval</response>
        [HttpGet("sites")]
        [ProducesResponseType(typeof(IEnumerable<SiteConfigResponse>), 200)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Get configuration for a specific industrial site
        /// </summary>
        /// <remarks>
        /// Retrieves detailed configuration information for a single site including all
        /// layout settings, geographic positioning, and display preferences. This endpoint
        /// is used for site-specific configuration management and layout customization.
        /// 
        /// Detailed Configuration Includes:
        /// - Complete site identification and naming
        /// - Precise geographic coordinates for map positioning
        /// - Layout mode selection and custom layout file references
        /// - Grid configuration with column count, spacing, and style preferences
        /// - Sensor grouping and labeling display options
        /// 
        /// Grid Configuration Options:
        /// - Columns: 0 for auto-calculation, 1-20 for fixed column count
        /// - Style: "square", "circle", or "hexagon" sensor representations
        /// - Spacing: 0-50% spacing between sensor elements
        /// - GroupByType: Boolean flag for grouping sensors by detector type
        /// - ShowLabels: Boolean flag for displaying sensor labels
        /// </remarks>
        /// <param name="id">Site ID (1-12 for current industrial facilities)</param>
        /// <returns>Detailed configuration for the specified site</returns>
        /// <response code="200">Successfully retrieved site configuration</response>
        /// <response code="404">Site not found</response>
        /// <response code="500">Internal server error during configuration retrieval</response>
        [HttpGet("sites/{id:int}")]
        [ProducesResponseType(typeof(SiteConfigResponse), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Update configuration settings for an existing site
        /// </summary>
        /// <remarks>
        /// Updates site configuration including geographic positioning, layout mode selection,
        /// and grid display preferences. This endpoint enables dynamic reconfiguration of
        /// site display properties without requiring system restart or client reconnection.
        /// 
        /// Updatable Configuration:
        /// - Site naming and county assignment
        /// - Map positioning coordinates (0-100% on county maps)
        /// - Layout display mode (svg/grid/auto)
        /// - Custom layout file references
        /// - Grid arrangement preferences and styling options
        /// 
        /// Coordinate Validation:
        /// - MapX and MapY coordinates must be between 0 and 100 (percentage)
        /// - Coordinates represent positioning on Romanian county maps
        /// - Invalid coordinates will result in HTTP 400 validation error
        /// 
        /// Layout Mode Options:
        /// - "svg": Forces use of custom SVG layout (requires layoutFile)
        /// - "grid": Forces use of automatic grid arrangement
        /// - "auto": Intelligent fallback (SVG if available, otherwise grid)
        /// 
        /// Changes take effect immediately and are persisted to configuration files.
        /// </remarks>
        /// <param name="id">Site ID to update</param>
        /// <param name="request">Updated site configuration data</param>
        /// <returns>Updated site configuration</returns>
        /// <response code="200">Successfully updated site configuration</response>
        /// <response code="400">Invalid configuration data or validation errors</response>
        /// <response code="404">Site not found</response>
        /// <response code="500">Internal server error during configuration update</response>
        [HttpPut("sites/{id:int}")]
        [ProducesResponseType(typeof(SiteConfigResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
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

                if (request.GridConfig != null)
                {
                    existingSite.GridConfig.Columns = request.GridConfig.Columns;
                    existingSite.GridConfig.Style = request.GridConfig.Style;
                    existingSite.GridConfig.Spacing = request.GridConfig.Spacing;
                    existingSite.GridConfig.GroupByType = request.GridConfig.GroupByType;
                    existingSite.GridConfig.ShowLabels = request.GridConfig.ShowLabels;
                }

                // Save the updated site
                var success = await _configurationService.SaveSiteAsync(existingSite);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to save site configuration" });
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
        /// Create a new site configuration
        /// </summary>
        /// <remarks>
        /// Creates a new industrial monitoring site with complete configuration including
        /// geographic positioning, layout preferences, and display settings. This endpoint
        /// enables dynamic expansion of the monitoring system to new facilities.
        /// 
        /// Required Configuration:
        /// - Unique site ID and descriptive name
        /// - County assignment (Prahova or Gorj)
        /// - Map positioning coordinates (0-100% on county maps)
        /// - Layout mode selection (svg/grid/auto)
        /// - Grid configuration preferences
        /// 
        /// Site ID Guidelines:
        /// - Must be unique across the system
        /// - Typically sequential (next available number)
        /// - Used for database relations and file naming
        /// 
        /// After creation, the site will be available for:
        /// - Sensor assignment and positioning
        /// - Real-time monitoring data collection
        /// - Layout customization and display
        /// - Administrative management operations
        /// </remarks>
        /// <param name="request">New site configuration data</param>
        /// <returns>Created site configuration</returns>
        /// <response code="201">Successfully created site configuration</response>
        /// <response code="400">Invalid configuration data or validation errors</response>
        /// <response code="409">Site ID already exists</response>
        /// <response code="500">Internal server error during site creation</response>
        [HttpPost("sites")]
        [ProducesResponseType(typeof(SiteConfigResponse), 201)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 409)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<ActionResult<SiteConfigResponse>> CreateSiteConfiguration([FromBody] SiteCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating new site configuration with ID {SiteId}", request.Id);

                // Check if site already exists
                var existingSite = await _configurationService.GetSiteAsync(request.Id);
                if (existingSite != null)
                {
                    return Conflict(new { error = $"Site with ID {request.Id} already exists" });
                }

                // Create new site configuration
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

                var success = await _configurationService.SaveSiteAsync(newSite);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to save site configuration" });
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
                _logger.LogError(ex, "Error creating site configuration for {SiteId}", request.Id);
                return StatusCode(500, new { error = "Failed to create site configuration" });
            }
        }

        /// <summary>
        /// Delete a site configuration and all associated data
        /// </summary>
        /// <remarks>
        /// Permanently removes a site configuration from the system including all associated
        /// sensor positioning data and layout customizations. This operation is irreversible
        /// and will affect all clients currently monitoring the specified site.
        /// 
        /// Deletion Impact:
        /// - Site configuration removed from sites.json
        /// - All sensor positioning data for the site removed from sensors.json
        /// - Custom SVG layout files deleted from the layouts directory
        /// - Real-time monitoring data collection stops for the site
        /// - Client applications will no longer display the site
        /// 
        /// Warning: This operation cannot be undone. Ensure proper backup procedures
        /// are in place before deleting site configurations.
        /// 
        /// Use cases:
        /// - Decommissioning of industrial facilities
        /// - Site reconfiguration requiring complete reset
        /// - Administrative cleanup of test or obsolete sites
        /// </remarks>
        /// <param name="id">Site ID to delete</param>
        /// <returns>Success confirmation</returns>
        /// <response code="200">Successfully deleted site configuration</response>
        /// <response code="404">Site not found</response>
        /// <response code="500">Internal server error during site deletion</response>
        [HttpDelete("sites/{id:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<ActionResult> DeleteSiteConfiguration(int id)
        {
            try
            {
                _logger.LogInformation("Deleting site configuration for ID {SiteId}", id);

                var existingSite = await _configurationService.GetSiteAsync(id);
                if (existingSite == null)
                {
                    return NotFound(new { error = $"Site {id} not found" });
                }

                var success = await _configurationService.DeleteSiteAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to delete site configuration" });
                }

                return Ok(new { message = $"Site {id} configuration deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site configuration for {SiteId}", id);
                return StatusCode(500, new { error = "Failed to delete site configuration" });
            }
        }

        /// <summary>
        /// Get sensor positioning configurations for a specific site
        /// </summary>
        /// <remarks>
        /// Retrieves sensor layout positioning data for a specific site, including coordinate
        /// information for custom SVG layouts and grid-based arrangements. This data is essential
        /// for rendering sensors in their correct positions on site layout displays.
        /// 
        /// Sensor Position Data:
        /// - Channel identification and display naming
        /// - Layout coordinates (LayoutX, LayoutY) as percentages (0-100%)
        /// - Site association for multi-site sensor management
        /// - Display names for user-friendly sensor identification
        /// 
        /// Coordinate System:
        /// - LayoutX: Horizontal position (0% = left edge, 100% = right edge)
        /// - LayoutY: Vertical position (0% = top edge, 100% = bottom edge)
        /// - Percentage-based for responsive scaling across different display sizes
        /// 
        /// Use cases:
        /// - Layout rendering engines for sensor positioning
        /// - Sensor position editing interfaces
        /// - Custom SVG layout development and testing
        /// - Grid layout configuration and optimization
        /// </remarks>
        /// <param name="siteId">Site ID for sensor position retrieval</param>
        /// <returns>List of sensor positions for the specified site</returns>
        /// <response code="200">Successfully retrieved sensor configurations</response>
        /// <response code="500">Internal server error during sensor configuration retrieval</response>
        [HttpGet("sensors/site/{siteId:int}")]
        [ProducesResponseType(typeof(IEnumerable<SensorConfigDto>), 200)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Update sensor positioning for a site's layout configuration
        /// </summary>
        /// <remarks>
        /// Updates the positioning coordinates for multiple sensors within a site's layout,
        /// enabling dynamic repositioning for both custom SVG layouts and grid-based arrangements.
        /// Changes are immediately saved and affect all client displays of the site layout.
        /// 
        /// Bulk Update Features:
        /// - Update multiple sensor positions in a single API call
        /// - Coordinate validation ensures all positions are within valid ranges
        /// - Atomic operation ensures all positions update together or none at all
        /// - Immediate persistence to configuration files
        /// 
        /// Position Coordinate Requirements:
        /// - LayoutX and LayoutY must be between 0 and 100 (percentage values)
        /// - Coordinates represent positioning on the site layout display area
        /// - Invalid coordinates will result in HTTP 400 validation error
        /// 
        /// Layout Impact:
        /// - Custom SVG layouts: Sensors reposition to exact coordinates
        /// - Grid layouts: Coordinates influence automatic grid positioning
        /// - Auto layouts: Coordinates used when SVG layout is available
        /// 
        /// Use cases:
        /// - Interactive sensor positioning interfaces
        /// - Bulk sensor layout updates during site reconfiguration
        /// - Automated layout optimization and arrangement
        /// - Custom layout development and testing
        /// </remarks>
        /// <param name="siteId">Site ID for sensor position updates</param>
        /// <param name="request">Sensor position update data</param>
        /// <returns>Updated sensor configurations</returns>
        /// <response code="200">Successfully updated sensor positions</response>
        /// <response code="400">Invalid position data or validation errors</response>
        /// <response code="404">Site not found or no sensors configured</response>
        /// <response code="500">Internal server error during position update</response>
        [HttpPut("sensors/site/{siteId:int}")]
        [ProducesResponseType(typeof(IEnumerable<SensorConfigDto>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Get county configurations for administrative site organization
        /// </summary>
        /// <remarks>
        /// Retrieves the administrative organization of industrial sites by Romanian counties,
        /// providing the mapping between geographic regions and monitoring facilities.
        /// This data is essential for regional reporting and administrative management.
        /// 
        /// County Organization:
        /// - Prahova County: Primary operational area with majority of sites
        /// - Gorj County: Secondary operational area with additional facilities
        /// - Site counts and ID mappings for administrative reference
        /// 
        /// Administrative Features:
        /// - Read-only county configurations (county boundaries are static)
        /// - Site count summaries for capacity planning
        /// - Regional grouping for management reporting
        /// - Geographic distribution analysis support
        /// 
        /// Use cases:
        /// - Regional monitoring dashboards
        /// - Administrative reporting and site organization
        /// - Geographic analysis and regional performance metrics
        /// - Regulatory compliance reporting by administrative region
        /// </remarks>
        /// <returns>List of county configurations with site associations</returns>
        /// <response code="200">Successfully retrieved county configurations</response>
        /// <response code="500">Internal server error during county configuration retrieval</response>
        [HttpGet("counties")]
        [ProducesResponseType(typeof(IEnumerable<CountyConfigDto>), 200)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Validate complete system configuration for consistency and correctness
        /// </summary>
        /// <remarks>
        /// Performs comprehensive validation of all system configuration data including
        /// sites, sensors, counties, and their relationships. This endpoint ensures
        /// configuration integrity and identifies potential issues before they affect operations.
        /// 
        /// Validation Scope:
        /// - Site configuration completeness and coordinate validity
        /// - Sensor positioning data consistency and coordinate ranges
        /// - County-site associations and administrative mappings
        /// - Cross-references between configuration files
        /// - Layout file availability and accessibility
        /// 
        /// Validation Results:
        /// - Overall validity status (pass/fail)
        /// - Detailed error messages for configuration issues
        /// - Warning messages for potential concerns
        /// - Configuration statistics and counts
        /// 
        /// Common Validation Issues:
        /// - Invalid coordinate ranges (not between 0-100%)
        /// - Missing or inaccessible layout files
        /// - Orphaned sensor configurations without associated sites
        /// - Duplicate site IDs or naming conflicts
        /// - County-site association mismatches
        /// 
        /// Use cases:
        /// - Pre-deployment configuration verification
        /// - Regular configuration health monitoring
        /// - Troubleshooting configuration-related issues
        /// - Automated configuration testing and validation
        /// </remarks>
        /// <returns>Comprehensive configuration validation results</returns>
        /// <response code="200">Successfully completed configuration validation</response>
        /// <response code="500">Internal server error during configuration validation</response>
        [HttpGet("validate")]
        [ProducesResponseType(typeof(ConfigValidationDto), 200)]
        [ProducesResponseType(typeof(object), 500)]
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
        /// Get configuration system debug information for troubleshooting
        /// </summary>
        /// <remarks>
        /// Provides detailed diagnostic information about the configuration system including
        /// file paths, file existence status, and basic file metadata. This endpoint is
        /// essential for troubleshooting configuration loading issues and system diagnostics.
        /// 
        /// Debug Information Includes:
        /// - Configuration file paths (sites.json, sensors.json, counties.json)
        /// - File existence verification for each configuration file
        /// - File metadata (size, last modified timestamp)
        /// - Sample file content for verification of file accessibility
        /// 
        /// Troubleshooting Use Cases:
        /// - Configuration file loading failures
        /// - File permission or accessibility issues
        /// - Configuration deployment verification
        /// - System integration debugging
        /// 
        /// Warning: This endpoint may expose system path information and should be
        /// used only for administrative troubleshooting purposes.
        /// </remarks>
        /// <returns>Configuration system debug information</returns>
        /// <response code="200">Successfully retrieved debug information</response>
        /// <response code="500">Error occurred while gathering debug information</response>
        [HttpGet("debug")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
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
                    SitesFileInfo = System.IO.File.Exists(paths.SitesPath) ?
                    new
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

#region DTOs - Configuration Data Transfer Objects

/// <summary>
/// Site configuration response DTO with complete layout and positioning information
/// </summary>
public class SiteConfigResponse
{
    /// <summary>
    /// Unique site identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable site name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Romanian county assignment (Prahova or Gorj)
    /// </summary>
    public string County { get; set; } = string.Empty;

    /// <summary>
    /// Horizontal map coordinate (0-100% on county map)
    /// </summary>
    public double MapX { get; set; }

    /// <summary>
    /// Vertical map coordinate (0-100% on county map)
    /// </summary>
    public double MapY { get; set; }

    /// <summary>
    /// Layout display mode: "svg", "grid", or "auto"
    /// </summary>
    public string LayoutMode { get; set; } = "auto";

    /// <summary>
    /// Custom SVG layout filename (if applicable)
    /// </summary>
    public string LayoutFile { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if site has custom SVG layout available
    /// </summary>
    public bool HasCustomLayout { get; set; }

    /// <summary>
    /// Grid layout configuration settings
    /// </summary>
    public GridConfigResponse GridConfig { get; set; } = new();
}

/// <summary>
/// Grid layout configuration response DTO
/// </summary>
public class GridConfigResponse
{
    /// <summary>
    /// Number of columns (0 for auto-calculation)
    /// </summary>
    public int Columns { get; set; }

    /// <summary>
    /// Grid element style: "square", "circle", or "hexagon"
    /// </summary>
    public string Style { get; set; } = "square";

    /// <summary>
    /// Spacing between elements as percentage (0-50%)
    /// </summary>
    public double Spacing { get; set; }

    /// <summary>
    /// Group sensors by detector type
    /// </summary>
    public bool GroupByType { get; set; }

    /// <summary>
    /// Display sensor labels
    /// </summary>
    public bool ShowLabels { get; set; }
}

/// <summary>
/// Site configuration update request DTO with validation
/// </summary>
public class SiteUpdateRequest
{
    /// <summary>
    /// Site name (2-100 characters)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// County assignment (2-50 characters)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string County { get; set; } = string.Empty;

    /// <summary>
    /// Map X coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double MapX { get; set; }

    /// <summary>
    /// Map Y coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double MapY { get; set; }

    /// <summary>
    /// Layout mode selection
    /// </summary>
    [Required]
    public string LayoutMode { get; set; } = "auto";

    /// <summary>
    /// Optional custom layout filename
    /// </summary>
    public string? LayoutFile { get; set; }

    /// <summary>
    /// Grid configuration settings
    /// </summary>
    public GridConfigRequest? GridConfig { get; set; }
}

/// <summary>
/// Site creation request DTO with validation
/// </summary>
public class SiteCreateRequest
{
    /// <summary>
    /// Unique site ID (must be positive integer)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    /// <summary>
    /// Site name (2-100 characters)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// County assignment (2-50 characters)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string County { get; set; } = string.Empty;

    /// <summary>
    /// Map X coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double MapX { get; set; }

    /// <summary>
    /// Map Y coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double MapY { get; set; }

    /// <summary>
    /// Layout mode selection
    /// </summary>
    [Required]
    public string LayoutMode { get; set; } = "auto";

    /// <summary>
    /// Optional custom layout filename
    /// </summary>
    public string? LayoutFile { get; set; }

    /// <summary>
    /// Grid configuration settings
    /// </summary>
    public GridConfigRequest? GridConfig { get; set; }
}

/// <summary>
/// Grid configuration request DTO with validation
/// </summary>
public class GridConfigRequest
{
    /// <summary>
    /// Column count (0-20, 0 for auto)
    /// </summary>
    [Range(0, 20)]
    public int Columns { get; set; } = 0;

    /// <summary>
    /// Grid element style
    /// </summary>
    [Required]
    public string Style { get; set; } = "square";

    /// <summary>
    /// Element spacing percentage (0-50%)
    /// </summary>
    [Range(0, 50)]
    public double Spacing { get; set; } = 10.0;

    /// <summary>
    /// Group sensors by type
    /// </summary>
    public bool GroupByType { get; set; } = true;

    /// <summary>
    /// Show sensor labels
    /// </summary>
    public bool ShowLabels { get; set; } = true;
}

/// <summary>
/// Sensor configuration response DTO
/// </summary>
public class SensorConfigDto
{
    /// <summary>
    /// Associated site ID
    /// </summary>
    public int SiteId { get; set; }

    /// <summary>
    /// Sensor channel identifier
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Layout X coordinate (0-100%)
    /// </summary>
    public double LayoutX { get; set; }

    /// <summary>
    /// Layout Y coordinate (0-100%)
    /// </summary>
    public double LayoutY { get; set; }

    /// <summary>
    /// Human-readable sensor name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Sensor position update request DTO
/// </summary>
public class SensorUpdateRequest
{
    /// <summary>
    /// List of sensor position updates
    /// </summary>
    [Required]
    public List<SensorPositionDto> Sensors { get; set; } = new();
}

/// <summary>
/// Individual sensor position DTO with validation
/// </summary>
public class SensorPositionDto
{
    /// <summary>
    /// Sensor channel ID (2-10 characters)
    /// </summary>
    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Layout X coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double LayoutX { get; set; }

    /// <summary>
    /// Layout Y coordinate (0-100%)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public double LayoutY { get; set; }
}

/// <summary>
/// County configuration response DTO
/// </summary>
public class CountyConfigDto
{
    /// <summary>
    /// County name (Prahova or Gorj)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of site IDs in this county
    /// </summary>
    public List<int> SiteIds { get; set; } = new();

    /// <summary>
    /// Total number of sites in county
    /// </summary>
    public int SiteCount { get; set; }
}

/// <summary>
/// Configuration validation result DTO
/// </summary>
public class ConfigValidationDto
{
    /// <summary>
    /// Overall configuration validity
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of configuration errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of configuration warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Total number of configured sites
    /// </summary>
    public int SiteCount { get; set; }

    /// <summary>
    /// Total number of configured sensors
    /// </summary>
    public int SensorCount { get; set; }

    /// <summary>
    /// Total number of configured counties
    /// </summary>
    public int CountyCount { get; set; }
}

#endregion
///