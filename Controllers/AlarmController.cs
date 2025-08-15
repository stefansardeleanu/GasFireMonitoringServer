// File: Controllers/AlarmController.cs
// REST API controller for alarm data with comprehensive XML documentation

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.DTOs.Requests;
using GasFireMonitoringServer.Models.DTOs.Responses;
using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for managing alarm data from gas and fire monitoring sensors
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for retrieving, filtering, and managing alarm data
    /// from various gas and fire detection sensors across multiple industrial sites.
    /// All endpoints require Bearer token authentication.
    /// 
    /// Supported operations:
    /// - Advanced alarm filtering with date ranges and sensor tags
    /// - Site-specific alarm retrieval  
    /// - Active alarm monitoring (last 24 hours)
    /// - Comprehensive alarm statistics and trends
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AlarmController : ControllerBase
    {
        private readonly IAlarmService _alarmService;
        private readonly ILogger<AlarmController> _logger;

        /// <summary>
        /// Initializes a new instance of the AlarmController
        /// </summary>
        /// <param name="alarmService">Service for alarm business logic operations</param>
        /// <param name="logger">Logger for recording controller operations</param>
        public AlarmController(IAlarmService alarmService, ILogger<AlarmController> logger)
        {
            _alarmService = alarmService;
            _logger = logger;
        }

        /// <summary>
        /// Get alarms with advanced filtering options
        /// </summary>
        /// <remarks>
        /// Retrieves alarm data with comprehensive filtering capabilities including:
        /// - Site filtering by ID
        /// - Date range filtering (StartDate to EndDate)
        /// - Sensor tag filtering for specific sensors
        /// - Active-only filter for current alarms
        /// - Pagination support (Page/PageSize)
        /// - Sorting options (newest, oldest, severity)
        /// 
        /// Sample request:
        /// ```json
        /// {
        ///   "siteId": 5,
        ///   "startDate": "2024-01-01T00:00:00Z",
        ///   "endDate": "2024-01-31T23:59:59Z",
        ///   "activeOnly": false,
        ///   "maxResults": 100,
        ///   "sortBy": "newest"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Alarm filter parameters (optional, defaults to last 100 alarms)</param>
        /// <returns>List of alarms matching filter criteria with standardized API response format</returns>
        /// <response code="200">Successfully retrieved alarms with filtering applied</response>
        /// <response code="400">Invalid filter parameters (e.g., invalid date range, invalid site ID)</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during alarm retrieval</response>
        [HttpPost("filter")]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsFiltered(
            [FromBody] AlarmFilterRequestDto? request = null)
        {
            try
            {
                _logger.LogInformation("Getting alarms with filtering: {@Filter}", request);

                // Convert DTO to service filter
                var filter = ConvertToServiceFilter(request);

                var alarms = await _alarmService.GetAlarmsAsync(filter);

                // Convert entities to DTOs
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} alarms with filter", alarmDtos.Count);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    $"Retrieved {alarmDtos.Count} alarms"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid filter parameters: {@Filter}", request);
                return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms with filter");
                return StatusCode(500, ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                    "An error occurred while retrieving alarms"));
            }
        }

        /// <summary>
        /// Get alarms for a specific industrial site
        /// </summary>
        /// <remarks>
        /// Retrieves the most recent alarms for a specific site, ordered by timestamp (newest first).
        /// Useful for site-specific dashboards and monitoring screens.
        /// 
        /// Common use cases:
        /// - Site operator dashboards
        /// - Maintenance team site reviews
        /// - Site-specific alarm analysis
        /// </remarks>
        /// <param name="siteId">Site ID (1-12 for current industrial sites)</param>
        /// <param name="limit">Maximum number of alarms to return (1-1000, default: 100)</param>
        /// <returns>List of alarms for the specified site, ordered by newest first</returns>
        /// <response code="200">Successfully retrieved site alarms</response>
        /// <response code="400">Invalid site ID or limit parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="404">Site not found</response>
        /// <response code="500">Internal server error during alarm retrieval</response>
        [HttpGet("site/{siteId}")]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 404)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsBySite(
            [FromRoute] int siteId,
            [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alarms for site {SiteId} with limit {Limit}", siteId, limit);

                var alarms = await _alarmService.GetAlarmsBySiteAsync(siteId, limit);

                // Convert entities to DTOs
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} alarms for site {SiteId}", alarmDtos.Count, siteId);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    $"Retrieved {alarmDtos.Count} alarms for site {siteId}"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms for site {SiteId}", siteId);
                return StatusCode(500, ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                    "An error occurred while retrieving alarms"));
            }
        }

        /// <summary>
        /// Get comprehensive alarm statistics for dashboard displays
        /// </summary>
        /// <remarks>
        /// Provides detailed alarm statistics including:
        /// - Total alarm counts (all time, 24h, 7d, 30d)
        /// - Alarm frequency analysis by type
        /// - Daily trend data for charts
        /// - Most frequent alarm types
        /// - Alarms per day average
        /// 
        /// Perfect for:
        /// - Executive dashboards
        /// - Performance monitoring
        /// - Trend analysis reports
        /// - Maintenance planning
        /// </remarks>
        /// <param name="siteId">Optional site ID filter (omit for system-wide statistics)</param>
        /// <returns>Comprehensive alarm statistics object</returns>
        /// <response code="200">Successfully retrieved alarm statistics</response>
        /// <response code="400">Invalid site ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during statistics calculation</response>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponseDto<AlarmStats>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<AlarmStats>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<AlarmStats>), 500)]
        public async Task<ActionResult<ApiResponseDto<AlarmStats>>> GetAlarmStatistics([FromQuery] int? siteId = null)
        {
            try
            {
                _logger.LogInformation("Getting alarm statistics for site: {SiteId}", siteId?.ToString() ?? "All");

                var statistics = await _alarmService.GetAlarmStatsAsync(siteId);

                _logger.LogInformation("Retrieved alarm statistics successfully");

                return Ok(ApiResponseDto<AlarmStats>.SuccessResult(
                    statistics,
                    "Retrieved alarm statistics successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID for statistics: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<AlarmStats>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarm statistics");
                return StatusCode(500, ApiResponseDto<AlarmStats>.ErrorResult(
                    "An error occurred while retrieving alarm statistics"));
            }
        }

        /// <summary>
        /// Get currently active alarms (within last 24 hours)
        /// </summary>
        /// <remarks>
        /// Retrieves alarms that are currently considered "active" based on the last 24 hours.
        /// This endpoint is optimized for real-time monitoring dashboards and alert systems.
        /// 
        /// Active alarms are defined as alarms that occurred within the last 24 hours
        /// and may still require attention or acknowledgment.
        /// 
        /// Use cases:
        /// - Real-time monitoring dashboards
        /// - Alert notification systems
        /// - Operator console displays
        /// - Mobile monitoring apps
        /// </remarks>
        /// <param name="siteId">Optional site ID filter for site-specific active alarms</param>
        /// <returns>List of currently active alarms, ordered by newest first</returns>
        /// <response code="200">Successfully retrieved active alarms</response>
        /// <response code="400">Invalid site ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during active alarm retrieval</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetActiveAlarms([FromQuery] int? siteId = null)
        {
            try
            {
                _logger.LogInformation("Getting active alarms for site: {SiteId}", siteId?.ToString() ?? "All");

                var activeAlarms = await _alarmService.GetActiveAlarmsAsync(siteId);

                // Convert entities to DTOs
                var alarmDtos = activeAlarms.Select(ConvertToAlarmResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} active alarms", alarmDtos.Count);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    $"Retrieved {alarmDtos.Count} active alarms"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID for active alarms: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alarms");
                return StatusCode(500, ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                    "An error occurred while retrieving active alarms"));
            }
        }

        /// <summary>
        /// Get alarm trend analysis data for charting and pattern analysis
        /// </summary>
        /// <remarks>
        /// Provides daily alarm count data over a specified time period for trend analysis.
        /// The returned data is perfect for creating line charts, bar charts, and trend graphs
        /// in dashboards and reporting systems.
        /// 
        /// Data format:
        /// - Key: Date (DateTime) - each day in the analysis period
        /// - Value: Integer - number of alarms on that day
        /// 
        /// Use cases:
        /// - Performance trend charts
        /// - Monthly/weekly alarm reports
        /// - Predictive maintenance planning
        /// - Site performance comparisons
        /// </remarks>
        /// <param name="siteId">Optional site ID filter for site-specific trends</param>
        /// <param name="days">Number of days to analyze (1-365, default: 7)</param>
        /// <returns>Dictionary with dates as keys and alarm counts as values</returns>
        /// <response code="200">Successfully retrieved alarm trend data</response>
        /// <response code="400">Invalid days parameter (must be 1-365) or invalid site ID</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during trend analysis</response>
        [HttpGet("trends")]
        [ProducesResponseType(typeof(ApiResponseDto<Dictionary<DateTime, int>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<Dictionary<DateTime, int>>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<Dictionary<DateTime, int>>), 500)]
        public async Task<ActionResult<ApiResponseDto<Dictionary<DateTime, int>>>> GetAlarmTrends(
            [FromQuery] int? siteId = null,
            [FromQuery] int days = 7)
        {
            try
            {
                _logger.LogInformation("Getting alarm trends for site: {SiteId}, days: {Days}",
                    siteId?.ToString() ?? "All", days);

                if (days <= 0 || days > 365)
                {
                    return BadRequest(ApiResponseDto<Dictionary<DateTime, int>>.ErrorResult(
                        "Days must be between 1 and 365"));
                }

                var trends = await _alarmService.GetAlarmTrendsAsync(siteId, days);

                _logger.LogInformation("Retrieved alarm trends for {Days} days", days);

                return Ok(ApiResponseDto<Dictionary<DateTime, int>>.SuccessResult(
                    trends,
                    trends.Count,
                    $"Retrieved alarm trends for {days} days"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters for alarm trends");
                return BadRequest(ApiResponseDto<Dictionary<DateTime, int>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarm trends");
                return StatusCode(500, ApiResponseDto<Dictionary<DateTime, int>>.ErrorResult(
                    "An error occurred while retrieving alarm trends"));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convert AlarmFilterRequestDto to internal service AlarmFilter
        /// </summary>
        /// <param name="dto">Request DTO with filtering parameters</param>
        /// <returns>Internal service filter object</returns>
        private static AlarmFilter ConvertToServiceFilter(AlarmFilterRequestDto? dto)
        {
            if (dto == null)
                return new AlarmFilter();

            return new AlarmFilter
            {
                SiteId = dto.SiteId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                SensorTag = dto.SensorTag,
                Limit = dto.MaxResults,
                ActiveOnly = dto.ActiveOnly
            };
        }

        /// <summary>
        /// Convert Alarm entity to AlarmResponseDto for API response
        /// </summary>
        /// <param name="alarm">Alarm entity from database</param>
        /// <returns>DTO formatted for API response</returns>
        private static AlarmResponseDto ConvertToAlarmResponseDto(Alarm alarm)
        {
            // Extract alarm level and type from alarm message
            var (alarmTypeName, alarmLevel) = ParseAlarmMessage(alarm.AlarmMessage);

            return new AlarmResponseDto
            {
                Id = alarm.Id,
                SiteId = alarm.SiteId,
                SiteName = alarm.SiteName,
                SensorTag = alarm.SensorTag,
                ChannelId = ExtractChannelFromTag(alarm.SensorTag),
                AlarmTypeName = alarmTypeName,
                AlarmLevel = alarmLevel,
                Value = 0, // Not available in current alarm entity
                Units = "",
                Timestamp = alarm.Timestamp,
                RawMessage = alarm.RawMessage
            };
        }

        /// <summary>
        /// Parse alarm message to extract alarm type and level
        /// </summary>
        /// <param name="alarmMessage">Raw alarm message from MQTT</param>
        /// <returns>Tuple containing alarm type name and level</returns>
        private static (string typeName, int level) ParseAlarmMessage(string alarmMessage)
        {
            if (string.IsNullOrEmpty(alarmMessage))
                return ("Unknown", 1);

            // Extract alarm type
            string typeName = "General";
            if (alarmMessage.Contains("Gas", StringComparison.OrdinalIgnoreCase))
                typeName = "Gas";
            else if (alarmMessage.Contains("Fire", StringComparison.OrdinalIgnoreCase))
                typeName = "Fire";
            else if (alarmMessage.Contains("Fault", StringComparison.OrdinalIgnoreCase))
                typeName = "Fault";

            // Extract alarm level
            int level = 1; // Default
            if (alarmMessage.Contains("Level 2", StringComparison.OrdinalIgnoreCase))
                level = 2;
            else if (alarmMessage.Contains("Level 1", StringComparison.OrdinalIgnoreCase))
                level = 1;

            return (typeName, level);
        }

        /// <summary>
        /// Extract channel ID from sensor tag for display purposes
        /// </summary>
        /// <param name="sensorTag">Full sensor tag (e.g., "5_PanouHurezani_CH41")</param>
        /// <returns>Channel ID (e.g., "CH41")</returns>
        private static string ExtractChannelFromTag(string sensorTag)
        {
            if (string.IsNullOrEmpty(sensorTag))
                return "";

            // Extract channel from tag like "5_PanouHurezani_CH41"
            var parts = sensorTag.Split('_');
            return parts.Length > 2 ? parts[^1] : sensorTag;
        }

        #endregion
    }
}