// File: Controllers/SiteController.cs
// REST API controller for site information - REFACTORED to use service layer

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.DTOs.Responses;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing site information
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // All endpoints require authentication
    public class SiteController : ControllerBase
    {
        private readonly ISiteService _siteService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SiteController> _logger;

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
        /// Get all sites with their current status
        /// </summary>
        /// <returns>List of all sites with sensor counts and alarm status</returns>
        [HttpGet]
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
                    "An error occurred while retrieving sites"));
            }
        }

        /// <summary>
        /// Get sites grouped by county
        /// </summary>
        /// <returns>Sites organized by county with aggregated statistics</returns>
        [HttpGet("by-county")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetSitesByCounty()
        {
            try
            {
                _logger.LogInformation("Getting sites grouped by county");

                var countyGroups = await _siteService.GetSitesByCountyAsync();

                // Convert CountyGroup to response format using ACTUAL properties
                var response = countyGroups.Select(county => new
                {
                    county = county.CountyName, // CORRECT property name
                    totalSites = county.TotalSites,
                    onlineSites = county.OnlineSites,
                    offlineSites = county.OfflineSites,
                    statusBreakdown = new
                    {
                        normalCount = county.StatusBreakdown.NormalCount,
                        alarmCount = county.StatusBreakdown.AlarmCount,
                        faultCount = county.StatusBreakdown.FaultCount,
                        disabledCount = county.StatusBreakdown.DisabledCount,
                        offlineCount = county.StatusBreakdown.OfflineCount
                    },
                    sites = county.Sites.Select(ConvertToSiteResponseDto).ToList()
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
        /// Get detailed information for a specific site
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>Detailed site information including sensors and alarms</returns>
        [HttpGet("{id}")]
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

                // Convert SiteDetail to response format using ACTUAL properties
                var response = new
                {
                    // Using SiteInfo nested object
                    id = siteDetail.SiteInfo.Id,
                    name = siteDetail.SiteInfo.Name,
                    county = siteDetail.SiteInfo.County,
                    latitude = siteDetail.SiteInfo.Latitude,
                    longitude = siteDetail.SiteInfo.Longitude,
                    status = siteDetail.Status,
                    isOnline = siteDetail.IsOnline,
                    lastUpdate = siteDetail.LastUpdate,

                    // Using SensorStatistics nested object
                    statistics = new
                    {
                        totalSensors = siteDetail.SensorStatistics.TotalSensors,
                        normalSensors = siteDetail.SensorStatistics.NormalSensors,
                        alarmSensors = siteDetail.SensorStatistics.AlarmSensors,
                        faultSensors = siteDetail.SensorStatistics.FaultSensors,
                        disabledSensors = siteDetail.SensorStatistics.DisabledSensors,
                        onlineSensors = siteDetail.SensorStatistics.OnlineSensors,
                        offlineSensors = siteDetail.SensorStatistics.OfflineSensors,
                        sensorTypeBreakdown = siteDetail.SensorStatistics.SensorTypeBreakdown
                    },

                    // Recent alarms
                    recentAlarms = siteDetail.RecentAlarms.Take(10).Select(alarm => new
                    {
                        alarm.Id,
                        alarm.SensorTag,
                        alarm.AlarmMessage,
                        alarm.Timestamp
                    })
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
                _logger.LogError(ex, "Error retrieving site {SiteId}", id);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving site information"));
            }
        }

        /// <summary>
        /// Get overall system status summary
        /// </summary>
        /// <returns>System-wide status and health metrics</returns>
        [HttpGet("status-summary")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetStatusSummary()
        {
            try
            {
                _logger.LogInformation("Getting system status summary");

                var statusSummary = await _siteService.GetStatusSummaryAsync();

                // Convert StatusSummary using ACTUAL properties
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
        /// Convert SiteWithStatus to SiteResponseDto using ACTUAL properties
        /// </summary>
        private static SiteResponseDto ConvertToSiteResponseDto(SiteWithStatus siteWithStatus)
        {
            return new SiteResponseDto
            {
                Id = siteWithStatus.Id,
                Name = siteWithStatus.Name,
                County = siteWithStatus.County,
                Latitude = siteWithStatus.Latitude,
                Longitude = siteWithStatus.Longitude,
                Status = siteWithStatus.OverallStatus, // CORRECT property name
                TotalSensors = siteWithStatus.TotalSensors,
                NormalSensors = siteWithStatus.StatusBreakdown.NormalCount,
                AlarmSensors = siteWithStatus.StatusBreakdown.AlarmCount,
                ErrorSensors = siteWithStatus.StatusBreakdown.FaultCount, // Mapping FaultCount to ErrorSensors
                LastUpdate = siteWithStatus.LastUpdate ?? DateTime.MinValue, // Handle nullable DateTime
                HasCustomLayout = false, // This would need to come from configuration service
                LayoutMode = "grid" // Default value, would need configuration service lookup
            };
        }

        #endregion
    }
}