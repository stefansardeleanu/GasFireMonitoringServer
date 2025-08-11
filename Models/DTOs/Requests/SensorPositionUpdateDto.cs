// File: Models/DTOs/Requests/SensorPositionUpdateDto.cs
// Request DTO for updating sensor positions on layouts

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GasFireMonitoringServer.Models.DTOs.Requests
{
    /// <summary>
    /// Request DTO for updating sensor positions on layout coordinates
    /// </summary>
    public class SensorPositionUpdateDto
    {
        /// <summary>
        /// Site ID for the layout being updated
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Site ID must be positive")]
        public int SiteId { get; set; }

        /// <summary>
        /// List of sensor position updates
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one sensor position is required")]
        public List<SensorPositionDto> SensorPositions { get; set; } = new();

        /// <summary>
        /// Optional comment for this position update
        /// </summary>
        [StringLength(200)]
        public string? UpdateComment { get; set; }

        /// <summary>
        /// Whether to create a backup before applying changes
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        // Validation helpers
        public bool AreAllPositionsValid => SensorPositions.All(s => s.IsValidPosition);
        public bool HasDuplicateChannels => SensorPositions.GroupBy(s => s.ChannelId).Any(g => g.Count() > 1);
        public int TotalSensors => SensorPositions.Count;
    }

    /// <summary>
    /// Individual sensor position update
    /// </summary>
    public class SensorPositionDto
    {
        /// <summary>
        /// Channel ID of the sensor (e.g., "CH41")
        /// </summary>
        [Required]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Channel ID must be 2-10 characters")]
        public string ChannelId { get; set; } = "";

        /// <summary>
        /// X coordinate on layout (0-100 percentage)
        /// </summary>
        [Required]
        [Range(0, 100, ErrorMessage = "X coordinate must be between 0 and 100")]
        public double LayoutX { get; set; }

        /// <summary>
        /// Y coordinate on layout (0-100 percentage)
        /// </summary>
        [Required]
        [Range(0, 100, ErrorMessage = "Y coordinate must be between 0 and 100")]
        public double LayoutY { get; set; }

        /// <summary>
        /// Optional display name override
        /// </summary>
        [StringLength(50)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Whether this sensor should be visible on the layout
        /// </summary>
        public bool IsVisible { get; set; } = true;

        // Validation helper
        public bool IsValidPosition => LayoutX >= 0 && LayoutX <= 100 && LayoutY >= 0 && LayoutY <= 100;
    }
}