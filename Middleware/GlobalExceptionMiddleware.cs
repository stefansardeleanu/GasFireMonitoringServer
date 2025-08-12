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
            try
            {
                await _next(context);
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