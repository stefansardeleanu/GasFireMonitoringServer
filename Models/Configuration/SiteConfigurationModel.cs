// File: Models/Configuration/SiteConfigurationModel.cs
// Configuration model for site information

namespace GasFireMonitoringServer.Models.Configuration
{
    /// <summary>
    /// Site configuration model for JSON configuration files
    /// Contains only essential site properties for production deployment
    /// </summary>
    public class SiteConfigurationModel
    {
        /// <summary>
        /// Unique site identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Site name (e.g., "SondaMorEni", "PanouHurezani")
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// County where site is located (e.g., "Prahova", "Gorj")
        /// </summary>
        public string County { get; set; } = "";

        /// <summary>
        /// Site position on Romania map as percentage (0-100)
        /// Used for positioning site indicators on county map view
        /// </summary>
        public double MapX { get; set; }

        /// <summary>
        /// Site position on Romania map as percentage (0-100)
        /// Used for positioning site indicators on county map view
        /// </summary>
        public double MapY { get; set; }

        /// <summary>
        /// SVG layout file name for this site (optional)
        /// File should be stored in wwwroot/layouts/ folder
        /// Example: "site_1_layout.svg"
        /// If empty or file not found, system will use automatic grid layout
        /// </summary>
        public string LayoutFile { get; set; } = "";

        /// <summary>
        /// Layout mode for this site
        /// "svg" = Use SVG layout file, "grid" = Use automatic grid layout, "auto" = Try SVG first, fallback to grid
        /// </summary>
        public string LayoutMode { get; set; } = "auto";

        /// <summary>
        /// Grid layout configuration (used when LayoutMode is "grid" or as fallback)
        /// </summary>
        public GridLayoutConfig GridConfig { get; set; } = new();

        /// <summary>
        /// Validate configuration data
        /// </summary>
        public bool IsValid()
        {
            return Id > 0
                && !string.IsNullOrWhiteSpace(Name)
                && !string.IsNullOrWhiteSpace(County)
                && MapX >= 0 && MapX <= 100
                && MapY >= 0 && MapY <= 100
                && (string.IsNullOrEmpty(LayoutMode) ||
                    new[] { "svg", "grid", "auto" }.Contains(LayoutMode.ToLowerInvariant()));
        }
    }

    /// <summary>
    /// Grid layout configuration for sites without SVG layouts
    /// </summary>
    public class GridLayoutConfig
    {
        /// <summary>
        /// Number of columns in the grid (0 = auto-calculate)
        /// </summary>
        public int Columns { get; set; } = 0;

        /// <summary>
        /// Grid layout style: "square", "horizontal", "vertical", "compact"
        /// </summary>
        public string Style { get; set; } = "square";

        /// <summary>
        /// Spacing between sensors (percentage of container)
        /// </summary>
        public double Spacing { get; set; } = 10.0;

        /// <summary>
        /// Group sensors by type in grid
        /// </summary>
        public bool GroupByType { get; set; } = true;

        /// <summary>
        /// Show sensor labels in grid
        /// </summary>
        public bool ShowLabels { get; set; } = true;
    }
}
