// File: Models/DTOs/Requests/SensorFilterRequestDto.cs
// Request DTO for filtering sensor data

using System;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Models.DTOs.Requests
{
    /// <summary>
    /// Request DTO for filtering sensor queries
    /// </summary>
    public class SensorFilterRequestDto
    {
        /// <summary>
        /// Filter by specific site ID (optional)
        /// </summary>
        public int? SiteId { get; set; }

        /// <summary>
        /// Filter by detector type (optional)
        /// </summary>
        public int? DetectorType { get; set; }

        /// <summary>
        /// Filter by sensor status (0=Normal, 1-2=Alarm, 3+=Error) (optional)
        /// </summary>
        [Range(0, 10)]
        public int? Status { get; set; }

        /// <summary>
        /// Only show sensors currently in alarm state
        /// </summary>
        public bool AlarmOnly { get; set; } = false;

        /// <summary>
        /// Only show sensors with errors
        /// </summary>
        public bool ErrorOnly { get; set; } = false;

        /// <summary>
        /// Only show online sensors (updated within last 5 minutes)
        /// </summary>
        public bool OnlineOnly { get; set; } = false;

        /// <summary>
        /// Search by sensor tag name (partial match)
        /// </summary>
        [StringLength(50)]
        public string? TagSearch { get; set; }

        /// <summary>
        /// Search by channel ID (partial match)
        /// </summary>
        [StringLength(10)]
        public string? ChannelSearch { get; set; }

        /// <summary>
        /// Include layout positioning data
        /// </summary>
        public bool IncludeLayoutData { get; set; } = false;

        /// <summary>
        /// Maximum number of sensors to return
        /// </summary>
        [Range(1, 1000)]
        public int MaxResults { get; set; } = 200;

        /// <summary>
        /// Sort order: "channel", "tag", "status", "updated", "value"
        /// </summary>
        [StringLength(20)]
        public string SortBy { get; set; } = "channel";

        /// <summary>
        /// Sort direction: "asc" or "desc"
        /// </summary>
        [StringLength(4)]
        public string SortDirection { get; set; } = "asc";

        // Validation helpers
        public bool HasStatusFilter => Status.HasValue || AlarmOnly || ErrorOnly;
        public bool HasTextSearch => !string.IsNullOrWhiteSpace(TagSearch) || !string.IsNullOrWhiteSpace(ChannelSearch);
    }
}