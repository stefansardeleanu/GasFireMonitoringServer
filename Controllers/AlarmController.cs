// File: Controllers/AlarmController.cs
// REST API controller for alarm data - WORKING VERSION with minimal authentication added

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
    /// API controller for managing alarm data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ONLY CHANGE: Added authentication requirement
    public class AlarmController : ControllerBase
    {
        private readonly IAlarmService _alarmService;
        private readonly ILogger<AlarmController> _logger;

        public AlarmController(IAlarmService alarmService, ILogger<AlarmController> logger)
        {
            _alarmService = alarmService;
            _logger = logger;
        }

        /// <summary>
        /// Get alarms with filtering options
        /// </summary>
        /// <param name="request">Alarm filter parameters</param>
        /// <returns>List of alarms matching filter criteria</returns>
        [HttpPost("filter")]
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
        /// Get alarms for a specific site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="limit">Maximum number of alarms to return</param>
        /// <returns>List of alarms for the specified site</returns>
        [HttpGet("site/{siteId}")]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsBySite(
            int siteId,
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
        /// Get alarm statistics
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <returns>Alarm statistics</returns>
        [HttpGet("statistics")]
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
        /// Get active alarms (currently in alarm state)
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <returns>List of active alarms</returns>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetActiveAlarms([FromQuery] int? siteId = null)
        {
            try
            {
                _logger.LogInformation("Getting active alarms for site: {SiteId}", siteId?.ToString() ?? "All");

                var alarms = await _alarmService.GetActiveAlarmsAsync(siteId);

                // Convert entities to DTOs
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

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
        /// Get alarm trends for analysis
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="days">Number of days to analyze</param>
        /// <returns>Alarm trend data</returns>
        [HttpGet("trends")]
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
        /// Convert AlarmFilterRequestDto to service AlarmFilter
        /// </summary>
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
        /// Convert Alarm entity to AlarmResponseDto
        /// </summary>
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
        /// Parse alarm message to extract type and level
        /// </summary>
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
        /// Extract channel ID from sensor tag
        /// </summary>
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