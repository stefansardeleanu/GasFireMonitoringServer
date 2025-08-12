// File: Middleware/PerformanceLoggingMiddleware.cs
using System.Diagnostics;

namespace GasFireMonitoringServer.Middleware
{
    public class PerformanceLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceLoggingMiddleware> _logger;
        private const int SlowRequestThresholdMs = 1000; // 1 second

        public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (elapsedMs > SlowRequestThresholdMs)
                {
                    var username = context.User?.Identity?.Name ?? "Anonymous";
                    var userRole = context.User?.FindFirst("role")?.Value ?? "Unknown";

                    _logger.LogWarning("SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms for user {Username} ({Role})",
                        context.Request.Method,
                        context.Request.Path + context.Request.QueryString,
                        elapsedMs,
                        username,
                        userRole);
                }
            }
        }
    }
}