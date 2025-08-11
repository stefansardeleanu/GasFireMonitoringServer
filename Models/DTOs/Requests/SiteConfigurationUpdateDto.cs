// File: Models/DTOs/Requests/SiteConfigurationUpdateDto.cs
// Request DTO for updating site configuration

using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Models.DTOs.Requests
{
    /// <summary>
    /// Request DTO for updating site configuration changes
    /// </summary>
    public class SiteConfigurationUpdateDto
    {
        /// <summary>
        /// Site ID being updated
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Site ID must be positive")]
        public int SiteId { get; set; }

        /// <summary>
        /// Site name (optional update)
        /// </summary>
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Site name must be 2-100 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// County assignment (optional update)
        /// </summary>
        [StringLength(50)]
        public string? County { get; set; }

        /// <summary>
        /// X coordinate on Romania map (0-100 percentage) (optional update)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Map X coordinate must be between 0 and 100")]
        public double? MapX { get; set; }

        /// <summary>
        /// Y coordinate on Romania map (0-100 percentage) (optional update)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Map Y coordinate must be between 0 and 100")]
        public double? MapY { get; set; }

        /// <summary>
        /// Layout mode: "grid" or "svg" (optional update)
        /// </summary>
        [StringLength(10)]
        public string? LayoutMode { get; set; }

        /// <summary>
        /// SVG layout file name (optional update)
        /// </summary>
        [StringLength(100)]
        public string? LayoutFile { get; set; }

        /// <summary>
        /// Grid configuration for sites using grid layout (optional)
        /// </summary>
        public GridConfigurationUpdateDto? GridConfig { get; set; }

        /// <summary>
        /// Comment for this configuration change
        /// </summary>
        [StringLength(200)]
        public string? UpdateComment { get; set; }

        /// <summary>
        /// Whether to create a backup before applying changes
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        // Validation helpers
        public bool IsValidMapPosition =>
            (!MapX.HasValue && !MapY.HasValue) ||
            (MapX.HasValue && MapY.HasValue && MapX >= 0 && MapX <= 100 && MapY >= 0 && MapY <= 100);

        public bool IsValidLayoutMode =>
            string.IsNullOrEmpty(LayoutMode) ||
            LayoutMode == "grid" ||
            LayoutMode == "svg";
    }

    /// <summary>
    /// Grid configuration update for sites using grid layout
    /// </summary>
    public class GridConfigurationUpdateDto
    {
        /// <summary>
        /// Number of columns (0 = auto-calculate)
        /// </summary>
        [Range(0, 20, ErrorMessage = "Columns must be between 0 and 20")]
        public int Columns { get; set; } = 0;

        /// <summary>
        /// Grid style: "square", "circle", "hexagon"
        /// </summary>
        [StringLength(20)]
        public string Style { get; set; } = "square";

        /// <summary>
        /// Spacing between grid elements (percentage)
        /// </summary>
        [Range(0, 50, ErrorMessage = "Spacing must be between 0 and 50")]
        public double Spacing { get; set; } = 10.0;

        /// <summary>
        /// Group sensors by detector type
        /// </summary>
        public bool GroupByType { get; set; } = true;

        /// <summary>
        /// Show sensor labels on grid
        /// </summary>
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// Background color for grid layout
        /// </summary>
        [StringLength(20)]
        public string BackgroundColor { get; set; } = "#f5f5f5";

        /// <summary>
        /// Border color for grid elements
        /// </summary>
        [StringLength(20)]
        public string BorderColor { get; set; } = "#cccccc";
    }
}