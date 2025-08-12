// File: Middleware/PerformanceLoggingMiddleware.cs
using System.Diagnostics;
using Serilog.Context;

namespace GasFireMonitoringServer.Middleware
{
    /// <summary>
    /// Middleware to log slow operations and performance metrics
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

                    // Log performance metrics
                    using (LogContext.PushProperty("Performance", true))
                    using (LogContext.PushProperty("ElapsedMs", elapsedMs))
                    using (LogContext.PushProperty("StatusCode", statusCode))
                    using (LogContext.PushProperty("Method", method))
                    using (LogContext.PushProperty("Path", requestPath))
                    using (LogContext.PushProperty("Username", username))
                    using (LogContext.PushProperty("UserRole", userRole))
                    {
                        if (elapsedMs > VerySlowRequestThresholdMs)
                        {
                            _logger.LogError("VERY SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode}) for user {Username} ({Role})",
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

                    // Log specific endpoint performance
                    LogSpecificEndpointPerformance(method, requestPath, elapsedMs);
                }
            }
        }

        private void LogSpecificEndpointPerformance(string method, string path, long elapsedMs)
        {
            // Log performance for critical endpoints
            if (path.StartsWith("/api/sensor") && elapsedMs > 500)
            {
                _logger.LogInformation("Sensor API performance: {Method} {Path} took {ElapsedMs}ms", method, path, elapsedMs);
            }
            else if (path.StartsWith("/api/alarm") && elapsedMs > 800)
            {
                _logger.LogInformation("Alarm API performance: {Method} {Path} took {ElapsedMs}ms", method, path, elapsedMs);
            }
            else if (path.StartsWith("/api/site") && elapsedMs > 600)
            {
                _logger.LogInformation("Site API performance: {Method} {Path} took {ElapsedMs}ms", method, path, elapsedMs);
            }
            else if (path.StartsWith("/api/config") && elapsedMs > 300)
            {
                _logger.LogWarning("Configuration API slow: {Method} {Path} took {ElapsedMs}ms", method, path, elapsedMs);
            }
            else if (path.StartsWith("/api/layout") && elapsedMs > 400)
            {
                _logger.LogInformation("Layout API performance: {Method} {Path} took {ElapsedMs}ms", method, path, elapsedMs);
            }
        }
    }
}