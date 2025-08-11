// File: Models/DTOs/Common/ApiResponseDto.cs
// Standardized API response wrapper for consistent client communication

using System;
using System.Collections.Generic;

namespace GasFireMonitoringServer.Models.DTOs.Common
{
    /// <summary>
    /// Standardized API response wrapper for all endpoints
    /// Ensures consistent response format for client applications
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponseDto<T>
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// The actual data payload (null if operation failed)
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Count of items (useful for collections and pagination)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Server timestamp when response was generated
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata for debugging or client information
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        // Factory methods for easy creation

        /// <summary>
        /// Create successful response with data
        /// </summary>
        public static ApiResponseDto<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Count = data is System.Collections.ICollection collection ? collection.Count : 1
            };
        }

        /// <summary>
        /// Create successful response with data and custom count
        /// </summary>
        public static ApiResponseDto<T> SuccessResult(T data, int count, string message = "Operation completed successfully")
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Count = count
            };
        }

        /// <summary>
        /// Create error response
        /// </summary>
        public static ApiResponseDto<T> ErrorResult(string message, Dictionary<string, object>? metadata = null)
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Data = default(T),
                Count = 0,
                Metadata = metadata
            };
        }

        /// <summary>
        /// Create not found response
        /// </summary>
        public static ApiResponseDto<T> NotFoundResult(string message = "Resource not found")
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Data = default(T),
                Count = 0
            };
        }
    }
}