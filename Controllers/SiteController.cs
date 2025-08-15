// File: Controllers/SiteController.cs
// REST API controller for site information with comprehensive XML documentation

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.DTOs.Responses;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing industrial site information and monitoring status
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for retrieving and managing information about
    /// industrial monitoring sites across Prahova and Gorj counties in Romania.
    /// Each site contains multiple gas and fire detection sensors that are continuously monitored.
    /// All endpoints require Bearer token authentication.
    /// 
    /// Site Coverage:
    /// - Prahova County: 8 industrial sites (primary operational area)
    /// - Gorj County: 2 industrial sites (secondary operational area)
    /// - Total: 10+ active monitoring locations
    /// 
    /// Status Classifications:
    /// - Normal: All sensors operating within safe parameters
    /// - Alarm: One or more sensors in alarm state (Levels 1-2)
    /// - Fault: Hardware or communication faults detected (Status 3,5,6)
    /// - Disabled: Sensors in maintenance mode (Status 4)
    /// - Offline: No recent communication from site
    /// 
    /// Real-time Integration:
    /// - Continuous MQTT data processing from PLCs
    /// - Live sensor status aggregation
    /// - Automatic site status calculation based on sensor conditions
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class SiteController : ControllerBase
    {
        private readonly ISiteService _siteService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SiteController> _logger;

        /// <summary>
        /// Initializes a new instance of the SiteController
        /// </summary>
        /// <param name="siteService">Service for site business logic and status calculations</param>
        /// <param name="configurationService">Service for site configuration management</param>
        /// <param name="logger">Logger for recording controller operations</param>
        public SiteController(
            ISiteService siteService,
            IConfigurationService configurationService,
            ILogger<SiteController> logger)
        {
            _siteService = siteService;
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all industrial sites with their current operational status
        /// </summary>
        /// <remarks>
        /// Retrieves comprehensive information for all monitored industrial sites including
        /// real-time sensor status aggregation, alarm conditions, and operational health metrics.
        /// This endpoint is essential for system-wide monitoring dashboards and executive reporting.
        /// 
        /// Each site response includes:
        /// - Basic site information (name, county, geographic coordinates)
        /// - Real-time operational status derived from sensor data
        /// - Sensor count breakdown by status category (Normal/Alarm/Error)
        /// - Last communication timestamp and connectivity status
        /// - Layout configuration mode (SVG custom or auto-generated grid)
        /// 
        /// Status Calculation Logic:
        /// - Normal: All sensors operating within safe parameters
        /// - Alarm: One or more sensors in alarm state (critical safety condition)
        /// - Error: Hardware faults or communication issues detected
        /// - Offline: No recent communication from site PLCs
        /// 
        /// Use cases:
        /// - Main system monitoring dashboard
        /// - Executive safety reporting
        /// - Regional operations overview
        /// - Emergency response coordination
        /// - Maintenance planning and resource allocation
        /// </remarks>
        /// <returns>List of all sites with current operational status and sensor statistics</returns>
        /// <response code="200">Successfully retrieved all sites with current status</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during site status calculation</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDto<List<SiteResponseDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SiteResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<SiteResponseDto>>>> GetAllSites()
        {
            try
            {
                _logger.LogInformation("Getting all sites with status information");

                var sitesWithStatus = await _siteService.GetAllSitesAsync();

                // Convert to DTOs
                var siteDtos = sitesWithStatus.Select(ConvertToSiteResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} sites with status", siteDtos.Count);

                return Ok(ApiResponseDto<List<SiteResponseDto>>.SuccessResult(
                    siteDtos,
                    siteDtos.Count,
                    $"Retrieved {siteDtos.Count} sites successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sites");
                return StatusCode(500, ApiResponseDto<List<SiteResponseDto>>.ErrorResult(
                    "An error occurred while retrieving site information"));
            }
        }

        /// <summary>
        /// Get sites organized by county with regional status aggregation
        /// </summary>
        /// <remarks>
        /// Retrieves sites grouped by Romanian counties (Prahova and Gorj) with aggregated
        /// status information for regional monitoring and management. This endpoint provides
        /// the perfect data structure for regional dashboards and county-level reporting.
        /// 
        /// County Organization:
        /// - Prahova County: 8 sites (primary industrial area)
        /// - Gorj County: 2 sites (secondary operational area)
        /// 
        /// Regional Aggregation Features:
        /// - County-level status breakdown (sites by operational status)
        /// - Regional sensor count summaries
        /// - County-wide alarm and fault statistics
        /// - Regional connectivity health indicators
        /// - Geographic distribution of operational status
        /// 
        /// Use cases:
        /// - Regional operations dashboards
        /// - County-specific safety reports
        /// - Geographic risk assessment
        /// - Regional maintenance coordination
        /// - Management reporting by administrative region
        /// </remarks>
        /// <returns>Sites grouped by county with regional status aggregation</returns>
        /// <response code="200">Successfully retrieved sites by county</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during county aggregation</response>
        [HttpGet("by-county")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetSitesByCounty()
        {
            try
            {
                _logger.LogInformation("Getting sites grouped by county");

                var countyGroups = await _siteService.GetSitesByCountyAsync();

                // Convert to anonymous objects for response
                var response = countyGroups.Select(group => new
                {
                    countyName = group.CountyName,
                    totalSites = group.TotalSites,
                    onlineSites = group.OnlineSites,
                    offlineSites = group.OfflineSites,
                    statusBreakdown = new
                    {
                        normalSites = group.StatusBreakdown.NormalCount,
                        alarmSites = group.StatusBreakdown.AlarmCount,
                        faultSites = group.StatusBreakdown.FaultCount,
                        disabledSites = group.StatusBreakdown.DisabledCount
                    },
                    sites = group.Sites.Select(ConvertToSiteResponseDto).ToList()
                }).ToList();

                _logger.LogInformation("Retrieved sites for {CountyCount} counties", response.Count);

                return Ok(ApiResponseDto<object>.SuccessResult(
                    response,
                    response.Count,
                    $"Retrieved sites for {response.Count} counties"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites by county");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving sites by county"));
            }
        }

        /// <summary>
        /// Get detailed information for a specific industrial site
        /// </summary>
        /// <remarks>
        /// Retrieves comprehensive operational details for a single industrial site including
        /// all sensors, recent alarm history, and detailed status analysis. This endpoint is
        /// essential for site-specific operations, detailed troubleshooting, and maintenance activities.
        /// 
        /// Detailed Information Includes:
        /// - Complete site identification and location data
        /// - All associated sensors with current readings and status
        /// - Recent alarm history with timestamps and severity levels
        /// - Comprehensive sensor statistics and health metrics
        /// - Connectivity status and last communication timestamps
        /// - Layout configuration and display mode information
        /// 
        /// Status Analysis Features:
        /// - Real-time sensor count by operational status
        /// - Alarm frequency and pattern analysis
        /// - Hardware fault detection and reporting
        /// - Communication health monitoring
        /// - Maintenance requirement indicators
        /// 
        /// Use cases:
        /// - Site operator control rooms
        /// - Detailed troubleshooting and diagnostics
        /// - Maintenance team site inspections
        /// - Alarm investigation and root cause analysis
        /// - Site performance reporting and optimization
        /// </remarks>
        /// <param name="id">Site ID (1-12 for current industrial facilities)</param>
        /// <returns>Detailed site information with sensors, alarms, and comprehensive status analysis</returns>
        /// <response code="200">Successfully retrieved detailed site information</response>
        /// <response code="400">Invalid site ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="404">Site not found</response>
        /// <response code="500">Internal server error during site details retrieval</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetSite(int id)
        {
            try
            {
                _logger.LogInformation("Getting detailed information for site {SiteId}", id);

                if (id <= 0)
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "Site ID must be greater than 0"));
                }

                var siteDetail = await _siteService.GetSiteByIdAsync(id);

                if (siteDetail == null)
                {
                    _logger.LogWarning("Site {SiteId} not found", id);
                    return NotFound(ApiResponseDto<object>.NotFoundResult(
                        $"Site with ID {id} not found"));
                }

                // Create comprehensive response object
                var response = new
                {
                    siteInfo = new
                    {
                        id = siteDetail.SiteInfo.Id,
                        name = siteDetail.SiteInfo.Name,
                        county = siteDetail.SiteInfo.County,
                        latitude = siteDetail.SiteInfo.Latitude,
                        longitude = siteDetail.SiteInfo.Longitude
                    },
                    status = siteDetail.Status,
                    isOnline = siteDetail.IsOnline,
                    lastUpdate = siteDetail.LastUpdate,
                    sensorStatistics = new
                    {
                        totalSensors = siteDetail.SensorStatistics.TotalSensors,
                        normalSensors = siteDetail.SensorStatistics.NormalSensors,
                        alarmSensors = siteDetail.SensorStatistics.AlarmSensors,
                        faultSensors = siteDetail.SensorStatistics.FaultSensors,
                        disabledSensors = siteDetail.SensorStatistics.DisabledSensors,
                        onlineSensors = siteDetail.SensorStatistics.OnlineSensors,
                        offlineSensors = siteDetail.SensorStatistics.OfflineSensors,
                        lastUpdate = siteDetail.SensorStatistics.LastUpdate,
                        sensorTypeBreakdown = siteDetail.SensorStatistics.SensorTypeBreakdown
                    },
                    sensors = siteDetail.Sensors.Select(sensor => new
                    {
                        id = sensor.Id,
                        channelId = sensor.ChannelId,
                        tagName = sensor.TagName,
                        processValue = sensor.ProcessValue,
                        currentValue = sensor.CurrentValue,
                        status = sensor.Status,
                        statusText = sensor.StatusText,
                        detectorType = sensor.DetectorType,
                        units = sensor.Units,
                        lastUpdated = sensor.LastUpdated,
                        isOnline = (DateTime.UtcNow - sensor.LastUpdated).TotalMinutes < 5
                    }).ToList(),
                    recentAlarms = siteDetail.RecentAlarms.Select(alarm => new
                    {
                        id = alarm.Id,
                        sensorTag = alarm.SensorTag,
                        alarmMessage = alarm.AlarmMessage,
                        timestamp = alarm.Timestamp,
                        rawMessage = alarm.RawMessage
                    }).ToList()
                };

                _logger.LogInformation("Retrieved detailed information for site {SiteId}: {SiteName}",
                    id, siteDetail.SiteInfo.Name);

                return Ok(ApiResponseDto<object>.SuccessResult(
                    response,
                    $"Retrieved detailed information for site {siteDetail.SiteInfo.Name}"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID: {SiteId}", id);
                return BadRequest(ApiResponseDto<object>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site details for {SiteId}", id);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving site details"));
            }
        }

        /// <summary>
        /// Get comprehensive system status summary for executive dashboards
        /// </summary>
        /// <remarks>
        /// Provides high-level system status overview combining data from all sites for executive
        /// reporting and main dashboard displays. This endpoint delivers critical KPIs and health
        /// metrics that management needs for operational oversight and decision making.
        /// 
        /// System-wide Metrics Include:
        /// - Total site and sensor counts across the entire system
        /// - System health percentage based on operational status
        /// - Active alarm and fault condition summaries
        /// - Overall connectivity and communication health
        /// - Last system update timestamp for data freshness validation
        /// 
        /// Executive KPIs:
        /// - Percentage of sites operating normally
        /// - Total number of sites requiring immediate attention
        /// - System-wide sensor health and operational status
        /// - Critical alarm conditions requiring management awareness
        /// - Overall system availability and reliability metrics
        /// 
        /// Use cases:
        /// - Executive dashboards and C-level reporting
        /// - System health monitoring and SLA compliance
        /// - Emergency management coordination
        /// - Board reporting and compliance documentation
        /// - Strategic operations planning and resource allocation
        /// </remarks>
        /// <returns>Comprehensive system status summary with executive-level KPIs</returns>
        /// <response code="200">Successfully retrieved system status summary</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during system status calculation</response>
        [HttpGet("status-summary")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetStatusSummary()
        {
            try
            {
                _logger.LogInformation("Getting system status summary");

                var statusSummary = await _siteService.GetStatusSummaryAsync();

                // Create executive-level status summary response
                var response = new
                {
                    totalSites = statusSummary.TotalSites,
                    activeSites = statusSummary.ActiveSites,
                    offlineSites = statusSummary.OfflineSites,
                    sitesWithAlarms = statusSummary.SitesWithAlarms,
                    sitesWithFaults = statusSummary.SitesWithFaults,
                    sitesDisabled = statusSummary.SitesDisabled,
                    totalSensors = statusSummary.TotalSensors,
                    sensorsInAlarm = statusSummary.SensorsInAlarm,
                    sensorsWithFaults = statusSummary.SensorsWithFaults,
                    sensorsDisabled = statusSummary.SensorsDisabled,
                    lastSystemUpdate = statusSummary.LastSystemUpdate,
                    systemHealthPercentage = statusSummary.SystemHealthPercentage
                };

                _logger.LogInformation("Retrieved system status summary");

                return Ok(ApiResponseDto<object>.SuccessResult(
                    response,
                    "Retrieved system status summary successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system status summary");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving system status"));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convert SiteWithStatus business model to SiteResponseDto for API response
        /// </summary>
        /// <param name="siteWithStatus">Business model with calculated status and sensor aggregation</param>
        /// <returns>DTO formatted for API response with computed properties</returns>
        private static SiteResponseDto ConvertToSiteResponseDto(SiteWithStatus siteWithStatus)
        {
            return new SiteResponseDto
            {
                Id = siteWithStatus.Id,
                Name = siteWithStatus.Name,
                County = siteWithStatus.County,
                Latitude = siteWithStatus.Latitude,
                Longitude = siteWithStatus.Longitude,
                Status = siteWithStatus.OverallStatus,
                TotalSensors = siteWithStatus.TotalSensors,
                NormalSensors = siteWithStatus.StatusBreakdown.NormalCount,
                AlarmSensors = siteWithStatus.StatusBreakdown.AlarmCount,
                ErrorSensors = siteWithStatus.StatusBreakdown.FaultCount, // Mapping FaultCount to ErrorSensors for client compatibility
                LastUpdate = siteWithStatus.LastUpdate ?? DateTime.MinValue,
                HasCustomLayout = false, // This would need to come from configuration service
                LayoutMode = "grid" // Default value, would need configuration service lookup
            };
        }

        #endregion
    }
}