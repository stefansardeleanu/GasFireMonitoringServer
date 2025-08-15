// File: Middleware/PerformanceLoggingMiddleware.cs
// ENHANCED VERSION - Phase 7.3 Task 3: Integration with Performance Monitoring Service
// Enhanced performance tracking with comprehensive metrics collection

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using GasFireMonitoringServer.Services.Infrastructure;

namespace GasFireMonitoringServer.Middleware
{
    /// <summary>
    /// ENHANCED Middleware to log slow operations and feed performance monitoring service
    /// Phase 7.3 Enhancement: Integrates with comprehensive performance monitoring
    /// </summary>
    public class PerformanceLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceLoggingMiddleware> _logger;
        private const int SlowRequestThresholdMs = 1000; // 1 second
        private const int VerySlowRequestThresholdMs = 5000; // 5 seconds

        public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestPath = context.Request.Path + context.Request.QueryString;
            var method = context.Request.Method;
            var correlationId = context.TraceIdentifier;

            // Add correlation ID to log context
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();
                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    var statusCode = context.Response.StatusCode;

                    // Get user info for logging
                    var username = context.User?.Identity?.Name ?? "Anonymous";
                    var userRole = context.User?.FindFirst("role")?.Value ?? "Unknown";

                    // ENHANCEMENT: Record metrics in performance monitoring service
                    await RecordPerformanceMetrics(context, requestPath, method, statusCode, elapsedMs);

                    // Log performance metrics with enhanced context
                    using (LogContext.PushProperty("Performance", true))
                    using (LogContext.PushProperty("ElapsedMs", elapsedMs))
                    using (LogContext.PushProperty("StatusCode", statusCode))
                    using (LogContext.PushProperty("Method", method))
                    using (LogContext.PushProperty("Path", requestPath))
                    using (LogContext.PushProperty("Username", username))
                    using (LogContext.PushProperty("UserRole", userRole))
                    using (LogContext.PushProperty("ContentLength", context.Response.ContentLength ?? 0))
                    using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
                    {
                        if (elapsedMs > VerySlowRequestThresholdMs)
                        {
                            _logger.LogError("VERY SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode}) for user {Username} ({Role}) - PERFORMANCE ALERT",
                                method, requestPath, elapsedMs, statusCode, username, userRole);
                        }
                        else if (elapsedMs > SlowRequestThresholdMs)
                        {
                            _logger.LogWarning("SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode}) for user {Username} ({Role})",
                                method, requestPath, elapsedMs, statusCode, username, userRole);
                        }
                        else
                        {
                            _logger.LogDebug("REQUEST: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode}) for user {Username} ({Role})",
                                method, requestPath, elapsedMs, statusCode, username, userRole);
                        }
                    }

                    // ENHANCEMENT: Log detailed endpoint performance for critical APIs
                    LogSpecificEndpointPerformance(method, requestPath, elapsedMs, statusCode);
                }
            }
        }

        /// <summary>
        /// ENHANCEMENT: Record metrics in performance monitoring service
        /// </summary>
        private async Task RecordPerformanceMetrics(HttpContext context, string requestPath,
            string method, int statusCode, long elapsedMs)
        {
            try
            {
                // Get performance monitoring service from DI
                var performanceService = context.RequestServices.GetService<IPerformanceMonitoringService>();
                if (performanceService != null)
                {
                    // Record API request metrics
                    performanceService.RecordApiRequest(requestPath, method, statusCode, elapsedMs);

                    // ENHANCEMENT: Track endpoint-specific patterns
                    await TrackEndpointPatterns(performanceService, requestPath, method, statusCode, elapsedMs);
                }
            }
            catch (Exception ex)
            {
                // Don't let metrics recording break the request
                _logger.LogDebug(ex, "Error recording performance metrics for {Method} {Path}", method, requestPath);
            }
        }

        /// <summary>
        /// ENHANCEMENT: Track specific endpoint performance patterns
        /// </summary>
        private async Task TrackEndpointPatterns(IPerformanceMonitoringService performanceService,
            string path, string method, int statusCode, long elapsedMs)
        {
            // Categorize endpoints for specialized tracking
            var endpointCategory = CategorizeEndpoint(path);

            if (!string.IsNullOrEmpty(endpointCategory))
            {
                // Record category-specific metrics
                if (elapsedMs > GetCategoryThreshold(endpointCategory))
                {
                    _logger.LogInformation("Slow {Category} endpoint: {Method} {Path} took {ElapsedMs}ms",
                        endpointCategory, method, path, elapsedMs);
                }

                // Track error rates by category
                if (statusCode >= 400)
                {
                    _logger.LogWarning("{Category} endpoint error: {Method} {Path} returned {StatusCode}",
                        endpointCategory, method, path, statusCode);
                }
            }
        }

        /// <summary>
        /// ENHANCEMENT: Enhanced endpoint-specific performance logging
        /// </summary>
        private void LogSpecificEndpointPerformance(string method, string path, long elapsedMs, int statusCode)
        {
            var category = CategorizeEndpoint(path);
            var threshold = GetCategoryThreshold(category);

            if (elapsedMs > threshold)
            {
                switch (category)
                {
                    case "sensor":
                        _logger.LogInformation("Sensor API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "alarm":
                        _logger.LogWarning("Alarm API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "site":
                        _logger.LogInformation("Site API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "configuration":
                        _logger.LogWarning("Configuration API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "layout":
                        _logger.LogInformation("Layout API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "authentication":
                        _logger.LogWarning("Auth API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    case "health":
                        _logger.LogInformation("Health API Performance Alert: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms) - Status: {StatusCode}",
                            method, path, elapsedMs, threshold, statusCode);
                        break;

                    default:
                        if (elapsedMs > SlowRequestThresholdMs)
                        {
                            _logger.LogInformation("General API Performance Alert: {Method} {Path} took {ElapsedMs}ms - Status: {StatusCode}",
                                method, path, elapsedMs, statusCode);
                        }
                        break;
                }
            }

            // ENHANCEMENT: Track specific critical operations
            TrackCriticalOperations(method, path, elapsedMs, statusCode);
        }

        /// <summary>
        /// ENHANCEMENT: Track critical operations that impact safety monitoring
        /// </summary>
        private void TrackCriticalOperations(string method, string path, long elapsedMs, int statusCode)
        {
            // Real-time data endpoints (critical for safety)
            if (path.Contains("/api/sensor") && path.Contains("/alarms"))
            {
                if (elapsedMs > 200 || statusCode >= 400)
                {
                    _logger.LogError("CRITICAL: Alarm sensor endpoint performance issue - {Method} {Path} took {ElapsedMs}ms, Status: {StatusCode}",
                        method, path, elapsedMs, statusCode);
                }
            }

            // SignalR hub performance (critical for real-time updates)
            if (path.Contains("/monitoringHub"))
            {
                if (elapsedMs > 100 || statusCode >= 400)
                {
                    _logger.LogError("CRITICAL: SignalR hub performance issue - {Method} {Path} took {ElapsedMs}ms, Status: {StatusCode}",
                        method, path, elapsedMs, statusCode);
                }
            }

            // System status endpoints (critical for monitoring health)
            if (path.Contains("/status-summary") || path.Contains("/health"))
            {
                if (elapsedMs > 300 || statusCode >= 400)
                {
                    _logger.LogWarning("CRITICAL: System health endpoint issue - {Method} {Path} took {ElapsedMs}ms, Status: {StatusCode}",
                        method, path, elapsedMs, statusCode);
                }
            }

            // Configuration changes (critical for system updates)
            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                if (path.Contains("/api/config") || path.Contains("/api/layout"))
                {
                    _logger.LogInformation("Configuration Change: {Method} {Path} took {ElapsedMs}ms, Status: {StatusCode}",
                        method, path, elapsedMs, statusCode);
                }
            }
        }

        /// <summary>
        /// ENHANCEMENT: Categorize endpoints for specialized monitoring
        /// </summary>
        private string CategorizeEndpoint(string path)
        {
            if (path.StartsWith("/api/sensor")) return "sensor";
            if (path.StartsWith("/api/alarm")) return "alarm";
            if (path.StartsWith("/api/site")) return "site";
            if (path.StartsWith("/api/config")) return "configuration";
            if (path.StartsWith("/api/layout")) return "layout";
            if (path.StartsWith("/api/auth")) return "authentication";
            if (path.StartsWith("/api/health")) return "health";
            if (path.Contains("/monitoringHub")) return "signalr";

            return "general";
        }

        /// <summary>
        /// ENHANCEMENT: Get performance thresholds by endpoint category
        /// Different endpoints have different performance expectations
        /// </summary>
        private int GetCategoryThreshold(string category)
        {
            return category switch
            {
                "sensor" => 500,         // Sensor data should be fast
                "alarm" => 300,          // Alarms are critical - must be very fast
                "site" => 800,           // Site data can be more complex
                "configuration" => 200,   // Config should be cached and fast
                "layout" => 400,         // Layout files can take a bit longer
                "authentication" => 300, // Auth should be reasonably fast
                "health" => 150,         // Health checks must be very fast
                "signalr" => 100,        // Real-time connections must be instant
                _ => 1000               // General threshold
            };
        }
    }
}