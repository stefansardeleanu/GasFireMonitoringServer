// File: Controllers/AlarmController.cs
// REST API controller for alarm data - REFACTORED to use service layer

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
    // [Authorize] // All endpoints require authentication
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
        /// Get alarms with optional filtering
        /// </summary>
        /// <param name="filter">Filter criteria for alarms</param>
        /// <returns>List of alarms matching filter criteria</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarms([FromQuery] AlarmFilterRequestDto? filter = null)
        {
            try
            {
                _logger.LogInformation("Getting alarms with filter: SiteId={SiteId}, StartDate={StartDate}, EndDate={EndDate}",
                    filter?.SiteId, filter?.StartDate, filter?.EndDate);

                // Validate model if provided
                if (filter != null && !ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                        $"Invalid filter parameters: {string.Join(", ", errors)}"));
                }

                // Convert DTO filter to service filter
                var serviceFilter = ConvertToServiceFilter(filter);
                var alarms = await _alarmService.GetAlarmsAsync(serviceFilter);

                // Convert entities to DTOs
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} alarms", alarmDtos.Count);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    $"Retrieved {alarmDtos.Count} alarms successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for alarm query");
                return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms");
                return StatusCode(500, ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                    "An error occurred while retrieving alarms"));
            }
        }

        /// <summary>
        /// Get alarms for a specific site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="limit">Maximum number of records to return (default 50)</param>
        /// <returns>List of alarms for the site</returns>
        [HttpGet("site/{siteId}")]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsBySite(
            int siteId,
            [FromQuery] int limit = 50)
        {
            try
            {
                _logger.LogInformation("Getting alarms for site {SiteId}, limit {Limit}", siteId, limit);

                if (siteId <= 0)
                {
                    return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                        "Site ID must be greater than 0"));
                }

                if (limit <= 0 || limit > 1000)
                {
                    return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                        "Limit must be between 1 and 1000"));
                }

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
        /// <returns>Alarm statistics with trends and analysis</returns>
        [HttpGet("stats")]
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
        /// Get alarm history for a specific sensor
        /// </summary>
        /// <param name="sensorTag">Sensor tag</param>
        /// <param name="days">Number of days to look back (default 7)</param>
        /// <returns>List of alarms for the sensor</returns>
        [HttpGet("sensor/{sensorTag}")]
        public async Task<ActionResult<ApiResponseDto<List<AlarmResponseDto>>>> GetAlarmsBySensor(
            string sensorTag,
            [FromQuery] int days = 7)
        {
            try
            {
                _logger.LogInformation("Getting alarms for sensor {SensorTag} for last {Days} days", sensorTag, days);

                if (string.IsNullOrWhiteSpace(sensorTag))
                {
                    return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                        "Sensor tag cannot be empty"));
                }

                if (days <= 0 || days > 365)
                {
                    return BadRequest(ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                        "Days must be between 1 and 365"));
                }

                var alarms = await _alarmService.GetSensorAlarmHistoryAsync(sensorTag, days);

                // Convert entities to DTOs
                var alarmDtos = alarms.Select(ConvertToAlarmResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} alarms for sensor {SensorTag}", alarmDtos.Count, sensorTag);

                return Ok(ApiResponseDto<List<AlarmResponseDto>>.SuccessResult(
                    alarmDtos,
                    alarmDtos.Count,
                    $"Retrieved {alarmDtos.Count} alarms for sensor {sensorTag}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarms for sensor {SensorTag}", sensorTag);
                return StatusCode(500, ApiResponseDto<List<AlarmResponseDto>>.ErrorResult(
                    "An error occurred while retrieving sensor alarms"));
            }
        }

        /// <summary>
        /// Get alarm trends for dashboard analytics
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="days">Number of days to analyze (default 7)</param>
        /// <returns>Daily alarm count trends</returns>
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
                ChannelId = ExtractChannelFromTag(alarm.SensorTag), // Extract channel from sensor tag
                AlarmTypeName = alarmTypeName,
                AlarmLevel = alarmLevel,
                Value = 0.0, // Value not stored in alarm entity - could be extracted from sensor data if needed
                Units = "", // Units not stored in alarm entity
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
                return ("Unknown", 0);

            // Extract level from messages like "Alarm Level 1", "Alarm Level 2"
            if (alarmMessage.Contains("Level"))
            {
                var parts = alarmMessage.Split(' ');
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i].Equals("Level", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(parts[i + 1], out int level))
                        {
                            return ("Gas Alarm", level);
                        }
                    }
                }
            }

            // Handle other alarm types
            return alarmMessage switch
            {
                var msg when msg.Contains("Detector Error") => ("Detector Error", 3),
                var msg when msg.Contains("Detector Disabled") => ("Detector Disabled", 4),
                var msg when msg.Contains("Line Open") => ("Line Open Fault", 5),
                var msg when msg.Contains("Line Short") => ("Line Short Fault", 6),
                _ => (alarmMessage, 1)
            };
        }

        /// <summary>
        /// Extract channel ID from sensor tag (e.g., "KGD-002" -> "CH41")
        /// </summary>
        private static string ExtractChannelFromTag(string sensorTag)
        {
            // This is a simple implementation - you might need to adjust based on your tag naming convention
            // For now, return empty string as channel mapping would require sensor data lookup
            return "";
        }

        #endregion
    }
}