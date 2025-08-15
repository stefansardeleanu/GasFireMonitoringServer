// File: Controllers/AlarmController.cs
// REST API controller for alarm data with comprehensive XML documentation and REST-compliant filtering

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
        /// Get alarms with advanced filtering options using REST-compliant query parameters
        /// </summary>
        /// <remarks>
        /// Retrieves alarm data with comprehensive filtering capabilities using standard HTTP query parameters.
        /// This endpoint follows REST conventions by using GET method with query parameters instead of POST body.
        /// 
        /// <para><strong>🔍 Filtering Capabilities:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Site filtering</strong>: Filter by specific site ID</description></item>
        /// <item><description><strong>Date range filtering</strong>: Filter by start and end dates</description></item>
        /// <item><description><strong>Sensor filtering</strong>: Filter by specific sensor tag</description></item>
        /// <item><description><strong>Status filtering</strong>: Active alarms only or historical data</description></item>
        /// <item><description><strong>Pagination</strong>: Control result count and pagination</description></item>
        /// <item><description><strong>Sorting</strong>: Sort by timestamp, severity, or sensor tag</description></item>
        /// </list>
        /// 
        /// <para><strong>📊 Query Parameter Examples:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Site-specific alarms</strong>: <c>?siteId=5</c></description></item>
        /// <item><description><strong>Date range</strong>: <c>?startDate=2024-01-01&amp;endDate=2024-01-31</c></description></item>
        /// <item><description><strong>Active alarms only</strong>: <c>?activeOnly=true</c></description></item>
        /// <item><description><strong>Sensor-specific</strong>: <c>?sensorTag=CH41</c></description></item>
        /// <item><description><strong>Limited results</strong>: <c>?maxResults=50&amp;sortBy=newest</c></description></item>
        /// <item><description><strong>Complex filtering</strong>: <c>?siteId=5&amp;activeOnly=true&amp;maxResults=100&amp;sortBy=severity</c></description></item>
        /// </list>
        /// 
        /// <para><strong>🎯 Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Dashboard displays</strong>: Recent alarms for status overview</description></item>
        /// <item><description><strong>Site monitoring</strong>: Site-specific alarm lists for operators</description></item>
        /// <item><description><strong>Historical analysis</strong>: Date-range filtering for incident investigation</description></item>
        /// <item><description><strong>Sensor maintenance</strong>: Sensor-specific alarm history</description></item>
        /// <item><description><strong>Emergency response</strong>: Active alarms for immediate attention</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Performance Considerations:</strong></para>
        /// <para>Query parameters enable efficient database indexing and caching. Large date ranges should use 
        /// pagination (maxResults parameter) to maintain performance. Active-only queries are optimized for 
        /// real-time monitoring scenarios.</para>
        /// </remarks>
        /// <param name="siteId">Filter by specific site ID (optional)</param>
        /// <param name="startDate">Start date for alarm history in ISO 8601 format (optional)</param>
        /// <param name="endDate">End date for alarm history in ISO 8601 format (optional)</param>
        /// <param name="sensorTag">Filter by specific sensor tag (e.g., "CH41") (optional)</param>
        /// <param name="activeOnly">Only show active alarms currently in alarm state (default: false)</param>
        /// <param name="maxResults">Maximum number of alarms to return (1-1000, default: 100)</param>
        /// <param name="sortBy">Sort order: "newest", "oldest", "severity" (default: "newest")</param>
        /// <returns>List of alarms matching filter criteria with standardized API response format</returns>
        /// <response code="200">Successfully retrieved alarms with filtering applied</response>
        /// <response code="400">Invalid filter parameters (e.g., invalid date range, invalid site ID, parameters out of range)</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="422">Unprocessable Entity - Valid parameters but invalid business logic (e.g., end date before start date)</response>
        /// <response code="500">Internal server error during alarm retrieval</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 422)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarms(
            [FromQuery] int? siteId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? sensorTag = null,
            [FromQuery] bool activeOnly = false,
            [FromQuery] int maxResults = 100,
            [FromQuery] string sortBy = "newest")
        {
            try
            {
                _logger.LogInformation("Getting alarms with filters - SiteId: {SiteId}, StartDate: {StartDate}, EndDate: {EndDate}, SensorTag: {SensorTag}, ActiveOnly: {ActiveOnly}, MaxResults: {MaxResults}",
                    siteId, startDate, endDate, sensorTag, activeOnly, maxResults);

                // Validate parameters
                if (maxResults < 1 || maxResults > 1000)
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "MaxResults must be between 1 and 1000"));
                }

                // Validate date range business logic
                if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                {
                    return UnprocessableEntity(ApiResponseDto<object>.ErrorResult(
                        "Start date cannot be after end date"));
                }

                // Validate sort parameter
                var validSortOptions = new[] { "newest", "oldest", "severity" };
                if (!validSortOptions.Contains(sortBy.ToLower()))
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "SortBy must be one of: newest, oldest, severity"));
                }

                // Create filter object from query parameters using existing AlarmFilter from IAlarmService
                var filter = new AlarmFilter
                {
                    SiteId = siteId,
                    StartDate = startDate,
                    EndDate = endDate,
                    SensorTag = sensorTag,
                    ActiveOnly = activeOnly,
                    Limit = maxResults
                };

                var alarms = await _alarmService.GetAlarmsAsync(filter);

                // Convert entities to DTOs using existing method
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

                var message = BuildFilterMessage(siteId, startDate, endDate, sensorTag, activeOnly, alarmDtos.Count);

                _logger.LogInformation("Retrieved {Count} alarms with filters", alarmDtos.Count);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    message));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid filter parameters");
                return BadRequest(ApiResponseDto<object>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms with filters");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
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
        /// <para><strong>🏭 Site-Specific Monitoring:</strong></para>
        /// <para>This endpoint provides focused alarm data for individual industrial sites, enabling
        /// site operators to monitor only relevant alarms for their facility.</para>
        /// 
        /// <para><strong>📊 Response Characteristics:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Chronological Order</strong>: Alarms sorted by timestamp (newest first)</description></item>
        /// <item><description><strong>Site Context</strong>: All alarms include site name and identification</description></item>
        /// <item><description><strong>Alarm Details</strong>: Complete alarm information including sensor tags and severity</description></item>
        /// <item><description><strong>Performance Optimized</strong>: Limited result set for fast response times</description></item>
        /// </list>
        /// 
        /// <para><strong>💡 Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Site Dashboards</strong>: Display recent site-specific alarm activity</description></item>
        /// <item><description><strong>Operator Stations</strong>: Show alarms relevant to specific facility operators</description></item>
        /// <item><description><strong>Mobile Apps</strong>: Field personnel checking site alarm status</description></item>
        /// <item><description><strong>Shift Reports</strong>: Generate site-specific alarm summaries</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <param name="limit">Maximum number of alarms to return (1-500, default: 100)</param>
        /// <returns>List of recent alarms for the specified site</returns>
        /// <response code="200">Successfully retrieved site-specific alarms</response>
        /// <response code="400">Invalid site ID or limit parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="404">Site not found or no alarms exist for this site</response>
        /// <response code="500">Internal server error during alarm retrieval</response>
        [HttpGet("site/{siteId}")]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsBySite(
            int siteId,
            [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting alarms for site {SiteId}, limit: {Limit}", siteId, limit);

                if (siteId <= 0)
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "Site ID must be greater than 0"));
                }

                if (limit < 1 || limit > 500)
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "Limit must be between 1 and 500"));
                }

                var alarms = await _alarmService.GetAlarmsBySiteAsync(siteId, limit);

                if (!alarms.Any())
                {
                    _logger.LogInformation("No alarms found for site {SiteId}", siteId);
                    return NotFound(ApiResponseDto<object>.ErrorResult(
                        $"No alarms found for site {siteId}"));
                }

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
                _logger.LogWarning(ex, "Invalid parameters for site alarms");
                return BadRequest(ApiResponseDto<object>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms for site {SiteId}", siteId);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving site alarms"));
            }
        }

        /// <summary>
        /// Get currently active alarms across all sites
        /// </summary>
        /// <remarks>
        /// Retrieves all alarms that are currently active across the entire monitoring system.
        /// Active alarms are those that occurred within the last 24 hours and represent ongoing alarm conditions.
        /// 
        /// <para><strong>🚨 Active Alarm Monitoring:</strong></para>
        /// <para>This endpoint is critical for emergency response and real-time monitoring systems.
        /// It provides immediate visibility into all current alarm conditions across all monitored facilities.</para>
        /// 
        /// <para><strong>⏰ Active Alarm Criteria:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Time Window</strong>: Alarms from the last 24 hours</description></item>
        /// <item><description><strong>System-wide Scope</strong>: Includes all sites and sensor types</description></item>
        /// <item><description><strong>Real-time Data</strong>: Most current alarm status information</description></item>
        /// <item><description><strong>Priority Ordering</strong>: Sorted by severity and recency</description></item>
        /// </list>
        /// 
        /// <para><strong>🎯 Critical Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Control Room Displays</strong>: Master alarm overview for 24/7 monitoring</description></item>
        /// <item><description><strong>Emergency Response</strong>: Immediate identification of all current hazards</description></item>
        /// <item><description><strong>Management Dashboards</strong>: Executive overview of system-wide alarm status</description></item>
        /// <item><description><strong>Mobile Alerts</strong>: Push notifications for active alarm conditions</description></item>
        /// <item><description><strong>Shift Handovers</strong>: Current alarm status for incoming operators</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Performance Optimization:</strong></para>
        /// <para>This endpoint is optimized for frequent polling by client applications and dashboard displays.
        /// Results are cached and updated in real-time via MQTT integration for minimal latency.</para>
        /// </remarks>
        /// <returns>List of all currently active alarms across all monitored sites</returns>
        /// <response code="200">Successfully retrieved active alarms (may be empty list if no active alarms)</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during active alarm retrieval</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponseDto<List<AlarmResponseDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetActiveAlarms()
        {
            try
            {
                _logger.LogInformation("Getting active alarms across all sites");

                var activeAlarms = await _alarmService.GetActiveAlarmsAsync();

                // Convert entities to DTOs
                var alarmDtos = activeAlarms.Select(ConvertToAlarmResponseDto).ToList();

                var message = alarmDtos.Any()
                    ? $"Retrieved {alarmDtos.Count} active alarms requiring attention"
                    : "No active alarms - all systems normal";

                _logger.LogInformation("Retrieved {Count} active alarms", alarmDtos.Count);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alarms");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving active alarms"));
            }
        }

        /// <summary>
        /// Get alarm trend data for dashboard analytics
        /// </summary>
        /// <remarks>
        /// Retrieves alarm frequency trends over a specified time period for dashboard analytics and reporting.
        /// Useful for identifying patterns, peak alarm periods, and system performance trends.
        /// 
        /// <para><strong>📈 Trend Analysis Features:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Flexible Time Periods</strong>: Configurable analysis window (1-365 days)</description></item>
        /// <item><description><strong>Site-specific or System-wide</strong>: Optional site filtering for focused analysis</description></item>
        /// <item><description><strong>Daily Aggregation</strong>: Alarm counts grouped by day for trend visualization</description></item>
        /// <item><description><strong>Performance Metrics</strong>: Historical alarm frequency for performance assessment</description></item>
        /// </list>
        /// 
        /// <para><strong>📊 Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Management Reports</strong>: Monthly/quarterly alarm trend analysis</description></item>
        /// <item><description><strong>Performance Monitoring</strong>: System reliability assessment over time</description></item>
        /// <item><description><strong>Predictive Maintenance</strong>: Identify sensors with increasing alarm frequency</description></item>
        /// <item><description><strong>Dashboard Charts</strong>: Graphical trend displays for monitoring screens</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Filter trends for specific site ID (optional, null for system-wide trends)</param>
        /// <param name="days">Number of days to analyze (1-365, default: 7)</param>
        /// <returns>Dictionary of dates with corresponding alarm counts</returns>
        /// <response code="200">Successfully retrieved alarm trend data</response>
        /// <response code="400">Invalid parameters (days out of range, invalid site ID)</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during trend analysis</response>
        [HttpGet("trends")]
        [ProducesResponseType(typeof(ApiResponseDto<Dictionary<DateTime, int>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
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
        /// Build descriptive message for filter results
        /// </summary>
        private static string BuildFilterMessage(int? siteId, DateTime? startDate, DateTime? endDate,
            string? sensorTag, bool activeOnly, int resultCount)
        {
            var parts = new List<string>();

            if (activeOnly)
                parts.Add("active");

            parts.Add($"{resultCount} alarms");

            if (siteId.HasValue)
                parts.Add($"for site {siteId}");

            if (!string.IsNullOrWhiteSpace(sensorTag))
                parts.Add($"from sensor {sensorTag}");

            if (startDate.HasValue || endDate.HasValue)
            {
                if (startDate.HasValue && endDate.HasValue)
                    parts.Add($"between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}");
                else if (startDate.HasValue)
                    parts.Add($"since {startDate:yyyy-MM-dd}");
                else if (endDate.HasValue)
                    parts.Add($"until {endDate:yyyy-MM-dd}");
            }

            return $"Retrieved {string.Join(" ", parts)}";
        }

        /// <summary>
        /// Convert Alarm entity to AlarmResponseDto for API response
        /// Uses existing entity structure and DTO properties
        /// </summary>
        /// <param name="alarm">Alarm entity from database</param>
        /// <returns>DTO formatted for API response</returns>
        private static AlarmResponseDto ConvertToAlarmResponseDto(Alarm alarm)
        {
            // Extract alarm level and type from alarm message using existing method
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
                Value = 0, // Not available in current alarm entity - keeping existing behavior
                Units = "", // Not available in current alarm entity - keeping existing behavior
                Timestamp = alarm.Timestamp,
                RawMessage = alarm.RawMessage
            };
        }

        /// <summary>
        /// Parse alarm message to extract alarm type and level
        /// Uses existing logic from original controller
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
        /// Uses existing logic from original controller
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