// File: Services/Business/Interfaces/ILayoutService.cs
// Layout management service interface

using GasFireMonitoringServer.Models.Configuration;
using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Layout type enumeration
    /// </summary>
    public enum LayoutType
    {
        SVG,        // Custom SVG layout file
        Grid,       // Automatic grid layout
        Auto        // Try SVG first, fallback to grid
    }

    /// <summary>
    /// Layout information for a site
    /// </summary>
    public class SiteLayoutInfo
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public LayoutType LayoutType { get; set; }
        public string? SvgContent { get; set; }           // SVG file content (if using SVG)
        public GridLayoutConfig GridConfig { get; set; } = new();    // Grid configuration (if using grid)
        public List<SensorLayoutPosition> SensorPositions { get; set; } = new();
        public bool HasCustomLayout { get; set; }        // True if SVG layout exists
    }

    /// <summary>
    /// Sensor position information for layouts
    /// </summary>
    public class SensorLayoutPosition
    {
        public string SensorKey { get; set; } = "";      // SiteId_ChannelId
        public int SiteId { get; set; }
        public string ChannelId { get; set; } = "";
        public double X { get; set; }                     // Position X (0-100%)
        public double Y { get; set; }                     // Position Y (0-100%)
        public string DisplayName { get; set; } = "";
        public int DetectorType { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = "";
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// Grid layout generation result
    /// </summary>
    public class GridLayoutResult
    {
        public List<SensorLayoutPosition> Positions { get; set; } = new();
        public int Columns { get; set; }
        public int Rows { get; set; }
        public GridLayoutConfig Config { get; set; } = new();
    }

    /// <summary>
    /// Layout management service interface
    /// Handles both SVG layouts and automatic grid layouts
    /// </summary>
    public interface ILayoutService
    {
        /// <summary>
        /// Get layout information for a site
        /// Automatically determines whether to use SVG or grid layout
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Complete layout information</returns>
        Task<SiteLayoutInfo> GetSiteLayoutAsync(int siteId);

        /// <summary>
        /// Check if a site has a custom SVG layout file
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>True if SVG layout file exists</returns>
        Task<bool> HasCustomLayoutAsync(int siteId);

        /// <summary>
        /// Get SVG layout content for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>SVG content or null if not found</returns>
        Task<string?> GetSvgLayoutAsync(int siteId);

        /// <summary>
        /// Generate automatic grid layout for a site
        /// Used when no SVG layout is available
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="customConfig">Optional custom grid configuration</param>
        /// <returns>Generated grid layout</returns>
        Task<GridLayoutResult> GenerateGridLayoutAsync(int siteId, GridLayoutConfig? customConfig = null);

        /// <summary>
        /// Get sensor positions for a site (from configuration or generated grid)
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>List of sensor positions with real-time data</returns>
        Task<List<SensorLayoutPosition>> GetSensorPositionsAsync(int siteId);

        /// <summary>
        /// Update sensor position in configuration
        /// Only works for sites with configured sensor positions
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="x">New X position (0-100%)</param>
        /// <param name="y">New Y position (0-100%)</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateSensorPositionAsync(int siteId, string channelId, double x, double y);

        /// <summary>
        /// Upload or update SVG layout file for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="svgContent">SVG file content</param>
        /// <returns>Success indicator</returns>
        Task<bool> SaveSvgLayoutAsync(int siteId, string svgContent);

        /// <summary>
        /// Delete SVG layout file for a site (will fallback to grid)
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Success indicator</returns>
        Task<bool> DeleteSvgLayoutAsync(int siteId);

        /// <summary>
        /// Generate optimal grid configuration based on sensor count and types
        /// </summary>
        /// <param name="sensorCount">Number of sensors</param>
        /// <param name="aspectRatio">Container aspect ratio (width/height)</param>
        /// <returns>Optimized grid configuration</returns>
        GridLayoutConfig GenerateOptimalGridConfig(int sensorCount, double aspectRatio = 1.0);
    }
}