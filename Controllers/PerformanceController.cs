// File: Controllers/PerformanceController.cs
// PHASE 7.3 TASK 3: Performance Monitoring API Controller
// Provides real-time system performance metrics and health status

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Services.Infrastructure;
using System;
using System.Threading.Tasks;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// Performance monitoring and system health API controller
    /// Provides real-time insights into system performance, health metrics, and operational status
    /// Essential for system administrators and DevOps monitoring
    /// </summary>
    /// <remarks>
    /// This controller serves as the central hub for system performance monitoring in the 
    /// gas and fire monitoring system. It provides comprehensive metrics and health status
    /// information critical for maintaining system reliability and operational excellence.
    /// 
    /// Performance Monitoring Features:
    /// - Real-time API performance metrics and response times
    /// - Database query performance tracking and optimization insights
    /// - MQTT message processing rates and connection health
    /// - SignalR broadcasting efficiency and client connection analytics
    /// - System resource utilization (CPU, memory, connections)
    /// 
    /// System Health Monitoring:
    /// - Database connectivity and response time validation
    /// - MQTT broker connection status and message throughput
    /// - Application memory usage and performance trends
    /// - Overall system health scoring and status classification
    /// 
    /// Operational Intelligence:
    /// - Performance trend analysis for capacity planning
    /// - Error rate monitoring and alerting thresholds
    /// - Resource utilization patterns for optimization
    /// - System bottleneck identification and resolution guidance
    /// 
    /// Target Audience:
    /// - System Administrators: Monitor overall system health and performance
    /// - DevOps Engineers: Track performance metrics and identify optimization opportunities
    /// - Support Teams: Troubleshoot performance issues and system problems
    /// - Management: Executive-level system health and reliability reporting
    /// 
    /// Security Note: These endpoints provide sensitive system information and should be
    /// restricted to authorized personnel only. All endpoints require proper authentication.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceMonitoringService _performanceService;
        private readonly ILogger<PerformanceController> _logger;

        /// <summary>
        /// Initializes a new instance of the PerformanceController
        /// </summary>
        /// <param name="performanceService">Service for performance metrics and monitoring</param>
        /// <param name="logger">Logger for recording controller operations</param>
        public PerformanceController(
            IPerformanceMonitoringService performanceService,
            ILogger<PerformanceController> logger)
        {
            _performanceService = performanceService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive system health status including all critical components
        /// </summary>
        /// <remarks>
        /// Retrieves real-time system health information including database connectivity,
        /// MQTT broker status, system resources, and overall operational health.
        /// This endpoint is essential for monitoring dashboards and automated health checks.
        /// 
        /// Health Components Monitored:
        /// - Database: Connection status, response times, data integrity
        /// - MQTT: Broker connectivity, message processing rates, error rates
        /// - System: Memory usage, CPU utilization, active connections
        /// - Application: Uptime, performance metrics, error rates
        /// 
        /// Health Status Classifications:
        /// - Healthy: All components operating within normal parameters
        /// - Degraded: Some components experiencing issues but system operational
        /// - Unhealthy: Critical components failing, immediate attention required
        /// 
        /// Use Cases:
        /// - Automated monitoring and alerting systems
        /// - Operations dashboard health indicators
        /// - Troubleshooting and diagnostics
        /// - SLA compliance and availability reporting
        /// </remarks>
        /// <returns>Comprehensive system health status with component details</returns>
        /// <response code="200">Successfully retrieved system health status</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during health check</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetSystemHealth()
        {
            try
            {
                _logger.LogDebug("Retrieving system health status");

                var healthStatus = await _performanceService.GetSystemHealthAsync();

                _logger.LogInformation("System health check completed - Overall Status: {OverallStatus}, " +
                    "DB: {DatabaseStatus}, MQTT: {MqttStatus}, Memory: {MemoryUsage}MB",
                    healthStatus.OverallStatus,
                    healthStatus.DatabaseHealth.Status,
                    healthStatus.MqttHealth.Status,
                    healthStatus.SystemHealth.MemoryUsageMB);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "System health status retrieved successfully",
                    Data = healthStatus,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system health status");

                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve system health status",
                    Data = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get comprehensive performance metrics including API, database, and MQTT statistics
        /// </summary>
        /// <remarks>
        /// Retrieves detailed performance metrics across all system components including
        /// API response times, database query performance, MQTT message processing,
        /// and SignalR broadcasting efficiency.
        /// 
        /// Performance Metrics Categories:
        /// - API Performance: Request counts, response times, error rates by endpoint
        /// - Database Performance: Query execution times, connection health, slow query detection
        /// - MQTT Performance: Message processing rates, connection stability, error rates
        /// - SignalR Performance: Broadcasting efficiency, client connection analytics
        /// - System Performance: Memory usage, CPU utilization, resource trends
        /// 
        /// Metric Types:
        /// - Counters: Cumulative values (total requests, total errors)
        /// - Gauges: Current values (active connections, memory usage)
        /// - Rates: Time-based calculations (messages per minute, requests per second)
        /// 
        /// Time Periods:
        /// - Real-time: Current values and immediate statistics
        /// - Recent: Last hour statistics for trend analysis
        /// - Historical: Performance trends over longer periods
        /// 
        /// Use Cases:
        /// - Performance monitoring dashboards
        /// - Capacity planning and resource optimization
        /// - Bottleneck identification and resolution
        /// - SLA monitoring and performance reporting
        /// </remarks>
        /// <returns>Comprehensive performance metrics across all system components</returns>
        /// <response code="200">Successfully retrieved performance metrics</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during metrics collection</response>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetPerformanceMetrics()
        {
            try
            {
                _logger.LogDebug("Retrieving performance metrics");

                var metrics = await _performanceService.GetPerformanceMetricsAsync();

                _logger.LogInformation("Performance metrics retrieved - " +
                    "API Requests (1h): {ApiRequestsLastHour}, " +
                    "DB Queries (1h): {DbQueriesLastHour}, " +
                    "MQTT Messages (1h): {MqttMessagesLastHour}, " +
                    "Avg Response Time: {AvgResponseTime}ms",
                    metrics.GetValueOrDefault("api_requests_last_hour", 0),
                    metrics.GetValueOrDefault("db_queries_last_hour", 0),
                    metrics.GetValueOrDefault("mqtt_messages_last_hour", 0),
                    metrics.GetValueOrDefault("average_response_time", 0));

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Performance metrics retrieved successfully",
                    Data = metrics,
                    Count = metrics.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics");

                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to retrieve performance metrics",
                    Data = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get system performance summary for executive dashboards and high-level monitoring
        /// </summary>
        /// <remarks>
        /// Provides a consolidated view of system performance designed for executive dashboards,
        /// management reporting, and high-level system monitoring. This endpoint aggregates
        /// key performance indicators into an easily digestible format.
        /// 
        /// Executive KPIs Included:
        /// - System Availability: Overall uptime and operational status
        /// - Performance Health: Average response times and system efficiency
        /// - Error Rates: System reliability and error trending
        /// - Resource Utilization: Infrastructure usage and capacity
        /// - Operational Metrics: Key statistics for business operations
        /// 
        /// Summary Categories:
        /// - Current Status: Real-time system health and performance
        /// - Recent Trends: Performance patterns over the last hour/day
        /// - Critical Alerts: Issues requiring immediate attention
        /// - Capacity Indicators: Resource usage and scaling recommendations
        /// 
        /// Dashboard Integration:
        /// - Designed for automatic refresh in monitoring dashboards
        /// - Optimized payload size for efficient network usage
        /// - Consistent format for easy chart and graph integration
        /// - Color-coded status indicators for quick visual assessment
        /// 
        /// Use Cases:
        /// - Executive and management dashboards
        /// - NOC (Network Operations Center) displays
        /// - Automated monitoring and alerting systems
        /// - Performance reporting and SLA compliance
        /// </remarks>
        /// <returns>Executive-level system performance summary with key indicators</returns>
        /// <response code="200">Successfully retrieved performance summary</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        /// <response code="500">Internal server error during summary generation</response>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
        public async Task<ActionResult<ApiResponseDto<object>>> GetPerformanceSummary()
        {
            try
            {
                _logger.LogDebug("Generating performance summary");

                var healthStatus = await _performanceService.GetSystemHealthAsync();
                var metrics = await _performanceService.GetPerformanceMetricsAsync();

                // Create executive summary
                var summary = new
                {
                    systemStatus = new
                    {
                        overallHealth = healthStatus.OverallStatus,
                        databaseStatus = healthStatus.DatabaseHealth.Status,
                        mqttStatus = healthStatus.MqttHealth.Status,
                        uptimeMinutes = healthStatus.SystemHealth.UptimeMinutes
                    },
                    performance = new
                    {
                        averageResponseTime = metrics.GetValueOrDefault("average_response_time", 0),
                        errorRate = metrics.GetValueOrDefault("error_rate", 0),
                        apiRequestsLastHour = metrics.GetValueOrDefault("api_requests_last_hour", 0),
                        mqttMessageRate = healthStatus.MqttHealth.MessageRate
                    },
                    resources = new
                    {
                        memoryUsageMB = healthStatus.SystemHealth.MemoryUsageMB,
                        cpuUsagePercent = healthStatus.SystemHealth.CpuUsagePercent,
                        activeConnections = healthStatus.SystemHealth.ActiveConnections
                    },
                    alerts = GeneratePerformanceAlerts(healthStatus, metrics),
                    timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Performance summary generated - " +
                    "Status: {OverallStatus}, Avg Response: {AvgResponse}ms, " +
                    "Error Rate: {ErrorRate}%, Memory: {Memory}MB",
                    healthStatus.OverallStatus,
                    metrics.GetValueOrDefault("average_response_time", 0),
                    metrics.GetValueOrDefault("error_rate", 0),
                    healthStatus.SystemHealth.MemoryUsageMB);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Performance summary generated successfully",
                    Data = summary,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance summary");

                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to generate performance summary",
                    Data = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Record a manual performance test for system validation
        /// </summary>
        /// <remarks>
        /// Allows system administrators to manually record performance test results
        /// for validation, troubleshooting, or testing purposes.
        /// </remarks>
        /// <param name="testName">Name of the performance test</param>
        /// <param name="durationMs">Test duration in milliseconds</param>
        /// <returns>Confirmation of test recording</returns>
        /// <response code="200">Performance test recorded successfully</response>
        /// <response code="400">Invalid test parameters</response>
        /// <response code="401">Unauthorized - Bearer token required</response>
        [HttpPost("test")]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        public async Task<ActionResult<ApiResponseDto<object>>> RecordPerformanceTest(
            [FromQuery] string testName,
            [FromQuery] long durationMs)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testName) || durationMs < 0)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid test parameters",
                        Data = null,
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Recording manual performance test: {TestName} took {DurationMs}ms",
                    testName, durationMs);

                // Record the test as an API request for tracking
                _performanceService.RecordApiRequest($"/test/{testName}", "POST", 200, durationMs);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Performance test recorded successfully",
                    Data = new { testName, durationMs, timestamp = DateTime.UtcNow },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording performance test");

                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Failed to record performance test",
                    Data = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Generate performance alerts based on current metrics
        /// </summary>
        private object GeneratePerformanceAlerts(SystemHealthStatus health, Dictionary<string, object> metrics)
        {
            var alerts = new List<object>();

            // Memory usage alert
            if (health.SystemHealth.MemoryUsageMB > 1000) // 1GB threshold
            {
                alerts.Add(new
                {
                    type = "memory",
                    severity = health.SystemHealth.MemoryUsageMB > 2000 ? "high" : "medium",
                    message = $"High memory usage: {health.SystemHealth.MemoryUsageMB}MB",
                    timestamp = DateTime.UtcNow
                });
            }

            // Response time alert
            var avgResponseTime = Convert.ToDouble(metrics.GetValueOrDefault("average_response_time", 0));
            if (avgResponseTime > 1000)
            {
                alerts.Add(new
                {
                    type = "performance",
                    severity = avgResponseTime > 2000 ? "high" : "medium",
                    message = $"Slow average response time: {avgResponseTime:F0}ms",
                    timestamp = DateTime.UtcNow
                });
            }

            // Error rate alert
            var errorRate = Convert.ToDouble(metrics.GetValueOrDefault("error_rate", 0));
            if (errorRate > 5.0)
            {
                alerts.Add(new
                {
                    type = "errors",
                    severity = errorRate > 10.0 ? "high" : "medium",
                    message = $"High error rate: {errorRate:F1}%",
                    timestamp = DateTime.UtcNow
                });
            }

            // Database connectivity alert
            if (!health.DatabaseHealth.IsConnected)
            {
                alerts.Add(new
                {
                    type = "database",
                    severity = "high",
                    message = "Database connection lost",
                    timestamp = DateTime.UtcNow
                });
            }

            // MQTT connectivity alert
            if (!health.MqttHealth.IsConnected)
            {
                alerts.Add(new
                {
                    type = "mqtt",
                    severity = "high",
                    message = "MQTT broker connection lost",
                    timestamp = DateTime.UtcNow
                });
            }

            return alerts;
        }
    }
}