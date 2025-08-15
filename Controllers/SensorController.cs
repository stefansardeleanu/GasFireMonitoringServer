// File: Controllers/SensorController.cs
// REST API controller for sensor data with comprehensive XML documentation

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
    /// API controller for managing sensor data from gas and fire monitoring systems
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for retrieving and managing sensor data from
    /// various gas, fire, and environmental monitoring sensors across multiple industrial sites.
    /// All endpoints require Bearer token authentication.
    /// 
    /// Sensor Types Supported:
    /// - Gas Detectors (Type 1): Monitor for combustible gases
    /// - Fire Detectors (Type 2): Detect fire conditions  
    /// - Smoke Detectors (Type 3): Early fire warning systems
    /// - Temperature Sensors (Type 4): Environmental monitoring
    /// - Pressure Sensors (Type 5): Process monitoring
    /// 
    /// Status Codes:
    /// - 0: Normal operation
    /// - 1: Alarm Level 1 (warning threshold exceeded)
    /// - 2: Alarm Level 2 (critical threshold exceeded)
    /// - 3: Detector Error (hardware fault)
    /// - 4: Detector Disabled (maintenance mode)
    /// - 5: Line Open Fault (wiring issue)
    /// - 6: Line Short Fault (electrical short)
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class SensorController : ControllerBase
    {
        private readonly ISensorService _sensorService;
        private readonly ILogger<SensorController> _logger;

        /// <summary>
        /// Initializes a new instance of the SensorController
        /// </summary>
        /// <param name="sensorService">Service for sensor business logic operations</param>
        /// <param name="logger">Logger for recording controller operations</param>
        public SensorController(ISensorService sensorService, ILogger<SensorController> logger)
        {
            _sensorService = sensorService;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors across all industrial sites
        /// </summary>
        /// <remarks>
        /// Retrieves comprehensive sensor data from all monitored industrial sites.
        /// Each sensor includes real-time values, status information, and connectivity status.
        /// 
        /// The response includes:
        /// - Current process values and 4-20mA readings
        /// - Human-readable status text and detector type names
        /// - Online/offline status based on last update time
        /// - Site association and channel information
        /// 
        /// Use cases:
        /// - System-wide monitoring dashboards
        /// - Cross-site analysis and reporting
        /// - Maintenance team overview screens
        /// - Emergency response coordination
        /// </remarks>
        /// <returns>List of all sensors with current status and values</returns>
        /// <response code="200">Successfully retrieved all sensors</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during sensor retrieval</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 500)]
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
                _logger.LogError(ex, "Error retrieving all sensors");
                return StatusCode(500, ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                    "An error occurred while retrieving sensors"));
            }
        }

        /// <summary>
        /// Get all sensors for a specific industrial site
        /// </summary>
        /// <remarks>
        /// Retrieves sensor data for a specific site, providing detailed information about
        /// all monitoring equipment at that location. This is essential for site-specific
        /// operations and maintenance activities.
        /// 
        /// Each sensor response includes:
        /// - Real-time process values and current readings
        /// - Detector type classification and capabilities
        /// - Current alarm state and fault conditions
        /// - Last communication timestamp and online status
        /// 
        /// Common use cases:
        /// - Site operator control rooms
        /// - Maintenance team site inspections
        /// - Emergency response procedures
        /// - Site performance analysis
        /// </remarks>
        /// <param name="siteId">Site ID (1-12 for current industrial facilities)</param>
        /// <returns>List of sensors at the specified site</returns>
        /// <response code="200">Successfully retrieved site sensors</response>
        /// <response code="400">Invalid site ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="404">Site not found or no sensors configured</response>
        /// <response code="500">Internal server error during sensor retrieval</response>
        [HttpGet("site/{siteId}")]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 404)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<SensorResponseDto>>>> GetSensorsBySite(
            [FromRoute] int siteId)
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
                var sensorList = sensors.ToList();

                if (!sensorList.Any())
                {
                    _logger.LogWarning("No sensors found for site {SiteId}", siteId);
                    return NotFound(ApiResponseDto<List<SensorResponseDto>>.NotFoundResult(
                        $"No sensors found for site {siteId}"));
                }

                // Convert entities to DTOs
                var sensorDtos = sensorList.Select(ConvertToSensorResponseDto).ToList();

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
        /// Get detailed information for a specific sensor
        /// </summary>
        /// <remarks>
        /// Retrieves comprehensive details for a single sensor, including all current readings,
        /// status information, and operational parameters. This endpoint is typically used for
        /// detailed sensor inspection and troubleshooting activities.
        /// 
        /// The detailed response includes:
        /// - Complete sensor identification (tag name, channel ID, site location)
        /// - Current process value and 4-20mA current reading
        /// - Detailed status interpretation and detector type information
        /// - Connectivity status and last communication timestamp
        /// - Operational parameters and measurement units
        /// 
        /// Use cases:
        /// - Detailed sensor diagnostics
        /// - Maintenance troubleshooting
        /// - Calibration verification
        /// - Performance monitoring
        /// </remarks>
        /// <param name="id">Unique sensor ID</param>
        /// <returns>Detailed sensor information</returns>
        /// <response code="200">Successfully retrieved sensor details</response>
        /// <response code="400">Invalid sensor ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="404">Sensor not found</response>
        /// <response code="500">Internal server error during sensor retrieval</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDto<SensorResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<SensorResponseDto>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<SensorResponseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponseDto<SensorResponseDto>), 500)]
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
                    "An error occurred while retrieving sensor details"));
            }
        }

        /// <summary>
        /// Get all sensors currently in alarm state (warning or critical conditions)
        /// </summary>
        /// <remarks>
        /// Retrieves all sensors across the system that are currently in alarm states
        /// (Status 1 = Alarm Level 1, Status 2 = Alarm Level 2). This endpoint is critical
        /// for safety monitoring and emergency response systems.
        /// 
        /// Alarm conditions indicate:
        /// - Level 1: Warning threshold exceeded (requires attention)
        /// - Level 2: Critical threshold exceeded (requires immediate action)
        /// 
        /// The response prioritizes sensors by:
        /// - Alarm level (Level 2 alarms first)
        /// - Most recent alarm first
        /// - Site priority
        /// 
        /// Use cases:
        /// - Emergency response dashboards
        /// - Safety monitoring systems
        /// - Alarm acknowledgment workflows
        /// - Real-time safety alerts
        /// </remarks>
        /// <returns>List of sensors currently in alarm state, ordered by severity and time</returns>
        /// <response code="200">Successfully retrieved alarmed sensors</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during alarm sensor retrieval</response>
        [HttpGet("alarms")]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<List<SensorResponseDto>>), 500)]
        public async Task<ActionResult<ApiResponseDto<List<SensorResponseDto>>>> GetAlarmedSensors()
        {
            try
            {
                _logger.LogInformation("Getting sensors currently in alarm state");

                var alarmedSensors = await _sensorService.GetAlarmedSensorsAsync();

                // Convert entities to DTOs
                var sensorDtos = alarmedSensors.Select(ConvertToSensorResponseDto).ToList();

                _logger.LogInformation("Retrieved {Count} sensors in alarm state", sensorDtos.Count);

                return Ok(ApiResponseDto<List<SensorResponseDto>>.SuccessResult(
                    sensorDtos,
                    sensorDtos.Count,
                    $"Retrieved {sensorDtos.Count} sensors in alarm state"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alarmed sensors");
                return StatusCode(500, ApiResponseDto<List<SensorResponseDto>>.ErrorResult(
                    "An error occurred while retrieving alarmed sensors"));
            }
        }

        /// <summary>
        /// Get comprehensive sensor statistics for dashboard displays and reporting
        /// </summary>
        /// <remarks>
        /// Provides detailed statistical analysis of sensor status and performance for either
        /// a specific site or the entire system. This endpoint is essential for management
        /// dashboards, performance monitoring, and operational reporting.
        /// 
        /// Statistics include:
        /// - Total sensor counts by status category (Normal/Alarm/Fault/Disabled)
        /// - Online/offline connectivity status breakdown
        /// - Sensor type distribution (Gas/Fire/Smoke/Temperature/Pressure)
        /// - Last update timestamps and communication health
        /// - Calculated percentages for dashboard displays
        /// 
        /// Use cases:
        /// - Executive dashboards and KPI monitoring
        /// - Operational performance reports
        /// - Maintenance planning and scheduling
        /// - System health monitoring
        /// - Compliance and safety reporting
        /// </remarks>
        /// <param name="siteId">Optional site ID filter (omit for system-wide statistics)</param>
        /// <returns>Comprehensive sensor statistics object</returns>
        /// <response code="200">Successfully retrieved sensor statistics</response>
        /// <response code="400">Invalid site ID parameter</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during statistics calculation</response>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponseDto<SensorStats>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<SensorStats>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ApiResponseDto<SensorStats>), 500)]
        public async Task<ActionResult<ApiResponseDto<SensorStats>>> GetSensorStatistics([FromQuery] int? siteId = null)
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

                return Ok(ApiResponseDto<SensorStats>.SuccessResult(
                    statistics,
                    "Retrieved sensor statistics successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID for statistics: {SiteId}", siteId);
                return BadRequest(ApiResponseDto<SensorStats>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sensor statistics");
                return StatusCode(500, ApiResponseDto<SensorStats>.ErrorResult(
                    "An error occurred while retrieving sensor statistics"));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Convert Sensor entity to SensorResponseDto for API response
        /// </summary>
        /// <param name="sensor">Sensor entity from business service</param>
        /// <returns>DTO formatted for API response with computed properties</returns>
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
        /// Convert detector type ID to human-readable name for display purposes
        /// </summary>
        /// <param name="detectorType">Detector type code from PLC</param>
        /// <returns>Human-readable detector type name</returns>
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