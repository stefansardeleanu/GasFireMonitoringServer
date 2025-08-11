// File: Controllers/SensorController.cs
// REST API controller for sensor data - REFACTORED to use service layer

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
    /// API controller for managing sensor data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // All endpoints require authentication
    public class SensorController : ControllerBase
    {
        private readonly ISensorService _sensorService;
        private readonly ILogger<SensorController> _logger;

        public SensorController(ISensorService sensorService, ILogger<SensorController> logger)
        {
            _sensorService = sensorService;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors across all sites
        /// </summary>
        /// <returns>List of all sensors grouped by site</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<List<SensorResponseDto>>>> GetAllSensors()
        {
            try
            {
                _logger.LogInformation("Getting all sensors with business logic");

                var sensors = await _sensorService.GetAllSensorsAsync();

                // Convert entities to DTOs
                var sensorDtos = sensors.Select(ConvertToSensorResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} sensors", sensorDtos.Count);

                return Ok(ApiResponseDto<List<SensorResponseDto>>.SuccessResult(
                    sensorDtos,
                    sensorDtos.Count,
                    $"Retrieved {sensorDtos.Count} sensors successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors");
                return StatusCode(500, ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                    "An error occurred while retrieving sensors"));
            }
        }

        /// <summary>
        /// Get all sensors for a specific site
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <returns>List of sensors for the specified site</returns>
        [HttpGet("site/{siteId}")]
        public async Task<ActionResult<ApiResponseDto<List<SensorResponseDto>>>> GetSensorsBySite(int siteId)
        {
            try
            {
                _logger.LogInformation("Getting sensors for site {SiteId}", siteId);

                if (siteId <= 0)
                {
                    return BadRequest(ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                        "Site ID must be greater than 0"));
                }

                var sensors = await _sensorService.GetSensorsBySiteAsync(siteId);

                // Convert entities to DTOs
                var sensorDtos = sensors.Select(ConvertToSensorResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} sensors for site {SiteId}", sensorDtos.Count, siteId);

                return Ok(ApiResponseDto<List<SensorResponseDto>>.SuccessResult(
                    sensorDtos,
                    sensorDtos.Count,
                    $"Retrieved {sensorDtos.Count} sensors for site {siteId}"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<List<SensorResponseDto>>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors for site {SiteId}", siteId);
                return StatusCode(500, ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                    "An error occurred while retrieving sensors"));
            }
        }

        /// <summary>
        /// Get a specific sensor by ID
        /// </summary>
        /// <param name="id">Sensor ID</param>
        /// <returns>Sensor details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDto<SensorResponseDto>>> GetSensor(int id)
        {
            try
            {
                _logger.LogInformation("Getting sensor {SensorId}", id);

                if (id <= 0)
                {
                    return BadRequest(ApiResponseDto<SensorResponseDto>.ErrorResult(
                        "Sensor ID must be greater than 0"));
                }

                var sensor = await _sensorService.GetSensorByIdAsync(id);

                if (sensor == null)
                {
                    _logger.LogWarning("Sensor {SensorId} not found", id);
                    return NotFound(ApiResponseDto<SensorResponseDto>.NotFoundResult(
                        $"Sensor with ID {id} not found"));
                }

                // Convert entity to DTO
                var sensorDto = ConvertToSensorResponseDto(sensor);

                _logger.LogInformation("Retrieved sensor {SensorId}: {TagName}", id, sensor.TagName);

                return Ok(ApiResponseDto<SensorResponseDto>.SuccessResult(
                    sensorDto,
                    $"Retrieved sensor {sensor.TagName} successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensor {SensorId}", id);
                return StatusCode(500, ApiResponseDto<SensorResponseDto>.ErrorResult(
                    "An error occurred while retrieving the sensor"));
            }
        }

        /// <summary>
        /// Get sensors currently in alarm state
        /// </summary>
        /// <returns>List of sensors with active alarms</returns>
        [HttpGet("alarms")]
        public async Task<ActionResult<ApiResponseDto<List<SensorResponseDto>>>> GetSensorsInAlarm()
        {
            try
            {
                _logger.LogInformation("Getting sensors currently in alarm state");

                var sensors = await _sensorService.GetAlarmedSensorsAsync();

                // Convert entities to DTOs
                var sensorDtos = sensors.Select(ConvertToSensorResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} sensors in alarm state", sensorDtos.Count);

                return Ok(ApiResponseDto<List<SensorResponseDto>>.SuccessResult(
                    sensorDtos,
                    sensorDtos.Count,
                    $"Retrieved {sensorDtos.Count} sensors in alarm state"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensors in alarm state");
                return StatusCode(500, ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                    "An error occurred while retrieving sensors in alarm state"));
            }
        }

        /// <summary>
        /// Get sensor statistics for dashboard
        /// </summary>
        /// <param name="siteId">Optional site ID to filter statistics</param>
        /// <returns>Sensor count statistics by status and type</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetSensorStatistics([FromQuery] int? siteId = null)
        {
            try
            {
                _logger.LogInformation("Getting sensor statistics for site: {SiteId}", siteId?.ToString() ?? "All");

                SensorStats statistics;
                if (siteId.HasValue)
                {
                    statistics = await _sensorService.GetSensorStatsAsync(siteId.Value);
                }
                else
                {
                    statistics = await _sensorService.GetSystemSensorStatsAsync();
                }

                _logger.LogInformation("Retrieved sensor statistics successfully");

                return Ok(ApiResponseDto<object>.SuccessResult(
                    statistics,
                    "Retrieved sensor statistics successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID for statistics: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<object>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensor statistics");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving sensor statistics"));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convert Sensor entity to SensorResponseDto
        /// </summary>
        private static SensorResponseDto ConvertToSensorResponseDto(Sensor sensor)
        {
            return new SensorResponseDto
            {
                Id = sensor.Id,
                SiteId = sensor.SiteId,
                SiteName = sensor.SiteName,
                ChannelId = sensor.ChannelId,
                TagName = sensor.TagName,
                DetectorTypeName = GetDetectorTypeName(sensor.DetectorType),
                ProcessValue = sensor.ProcessValue,
                CurrentValue = sensor.CurrentValue,
                StatusText = sensor.StatusText,
                Units = sensor.Units,
                LastUpdated = sensor.LastUpdated,
                IsOnline = (DateTime.UtcNow - sensor.LastUpdated).TotalMinutes < 5
            };
        }

        /// <summary>
        /// Convert detector type ID to human-readable name
        /// </summary>
        private static string GetDetectorTypeName(int detectorType)
        {
            return detectorType switch
            {
                1 => "Gas Detector",
                2 => "Fire Detector",
                3 => "Smoke Detector",
                4 => "Temperature Sensor",
                5 => "Pressure Sensor",
                _ => $"Type {detectorType}"
            };
        }

        #endregion
    }
}