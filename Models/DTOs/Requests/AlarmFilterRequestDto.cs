// File: Models/DTOs/Requests/AlarmFilterRequestDto.cs
// Request DTO for filtering alarm data

using System;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Models.DTOs.Requests
{
    /// <summary>
    /// Request DTO for filtering alarm data
    /// </summary>
    public class AlarmFilterRequestDto
    {
        /// <summary>
        /// Filter by specific site ID (optional)
        /// </summary>
        public int? SiteId { get; set; }

        /// <summary>
        /// Filter by specific sensor tag (optional)
        /// </summary>
        [StringLength(50)]
        public string? SensorTag { get; set; }

        /// <summary>
        /// Start date for alarm history (optional)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for alarm history (optional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by alarm level (1=Low, 2=High, etc.) (optional)
        /// </summary>
        [Range(1, 10)]
        public int? AlarmLevel { get; set; }

        /// <summary>
        /// Only show active alarms (currently in alarm state)
        /// </summary>
        public bool ActiveOnly { get; set; } = false;

        /// <summary>
        /// Maximum number of alarms to return
        /// </summary>
        [Range(1, 1000)]
        public int MaxResults { get; set; } = 100;

        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(10, 500)]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Sort order: "newest", "oldest", "severity"
        /// </summary>
        [StringLength(20)]
        public string SortBy { get; set; } = "newest";

        // Validation helper
        public bool IsValidDateRange => !StartDate.HasValue || !EndDate.HasValue || StartDate.Value <= EndDate.Value;
    }
}