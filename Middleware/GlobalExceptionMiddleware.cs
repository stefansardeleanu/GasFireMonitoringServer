// File: Middleware/GlobalExceptionMiddleware.cs
using System.Net;
using System.Text.Json;
using GasFireMonitoringServer.Models.DTOs.Common;

namespace GasFireMonitoringServer.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip exception handling for Swagger resources to avoid false errors
            if (context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/swagger-ui"))
            {
                await _next(context);
                return;
            }

            try
            {
                await _next(context);
            }
            catch (TaskCanceledException)
            {
                // Client disconnected, don't log as error
                _logger.LogDebug("Request was cancelled: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // Don't send response if client disconnected
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 499; // Client Closed Request
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. Request: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Don't try to write response if it's already started
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.ContentType = "application/json";

            var response = ex switch
            {
                ArgumentException argEx => new ApiResponseDto<object>
                {
                    Success = false,
                    Message = argEx.Message,
                    Data = null,
                    Count = 0,
                    Timestamp = DateTime.UtcNow
                },
                UnauthorizedAccessException => new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Access denied",
                    Data = null,
                    Count = 0,
                    Timestamp = DateTime.UtcNow
                },
                _ => new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An internal server error occurred",
                    Data = null,
                    Count = 0,
                    Timestamp = DateTime.UtcNow
                }
            };

            context.Response.StatusCode = ex switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}