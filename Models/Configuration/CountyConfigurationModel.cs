// File: Models/Configuration/CountyConfigurationModel.cs
// Configuration model for county-site associations

namespace GasFireMonitoringServer.Models.Configuration
{
    /// <summary>
    /// County configuration model
    /// Contains only site associations - county boundaries handled by client-side Romania map
    /// </summary>
    public class CountyConfigurationModel
    {
        /// <summary>
        /// County name (e.g., "Prahova", "Gorj")
        /// Must match county names in sites.json
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// List of site IDs located in this county
        /// Must match site IDs in sites.json
        /// </summary>
        public List<int> Sites { get; set; } = new();

        /// <summary>
        /// Optional: County display name
        /// If empty, uses the Name property
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Optional: County description
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Validate configuration data
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name)
                && Sites != null
                && Sites.Any()
                && Sites.All(id => id > 0);
        }

        /// <summary>
        /// Get display name (uses Name if DisplayName is empty)
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;
        }
    }
}