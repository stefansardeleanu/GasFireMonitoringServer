// File: Models/DTOs/Responses/LayoutResponseDto.cs
// Response DTO for SVG layout and coordinate data

using System;
using System.Collections.Generic;

namespace GasFireMonitoringServer.Models.DTOs.Responses
{
    /// <summary>
    /// Response DTO for layout information including SVG content and sensor coordinates
    /// </summary>
    public class LayoutResponseDto
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string LayoutMode { get; set; } = ""; // "grid" or "svg"
        public bool HasCustomLayout { get; set; }
        public string? SvgContent { get; set; } // SVG file content (null for grid mode)
        public List<SensorLayoutDto> SensorPositions { get; set; } = new();
        public GridConfigurationDto? GridConfig { get; set; } // Grid settings (null for SVG mode)
        public DateTime LastModified { get; set; }
        public string LayoutFileName { get; set; } = "";

        // Computed properties for client convenience
        public bool IsGridLayout => LayoutMode == "grid";
        public bool IsSvgLayout => LayoutMode == "svg";
        public int TotalSensors => SensorPositions.Count;
        public bool IsValid => SensorPositions.All(s => s.IsValidPosition);
    }

    /// <summary>
    /// Grid configuration for sites without custom SVG layouts
    /// </summary>
    public class GridConfigurationDto
    {
        public int Columns { get; set; } = 0; // 0 = auto-calculate
        public string Style { get; set; } = "square"; // "square", "circle", "hexagon"
        public double Spacing { get; set; } = 10.0; // Spacing between elements (percentage)
        public bool GroupByType { get; set; } = true; // Group sensors by detector type
        public bool ShowLabels { get; set; } = true; // Show sensor labels
        public string BackgroundColor { get; set; } = "#f5f5f5";
        public string BorderColor { get; set; } = "#cccccc";
    }
}