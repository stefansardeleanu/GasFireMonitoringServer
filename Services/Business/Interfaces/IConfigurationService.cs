// File: Services/Business/Interfaces/IConfigurationService.cs
// Configuration service interface for JSON configuration management

using GasFireMonitoringServer.Models.Configuration;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Configuration service interface for managing JSON configuration files
    /// Replaces hardcoded site data with flexible configuration system
    /// </summary>
    public interface IConfigurationService
    {
        #region Site Configuration

        /// <summary>
        /// Get all site configurations
        /// </summary>
        /// <returns>List of all site configurations</returns>
        Task<IEnumerable<SiteConfigurationModel>> GetAllSitesAsync();

        /// <summary>
        /// Get site configuration by ID
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Site configuration or null if not found</returns>
        Task<SiteConfigurationModel?> GetSiteAsync(int siteId);

        /// <summary>
        /// Get sites for a specific county
        /// </summary>
        /// <param name="county">County name</param>
        /// <returns>List of sites in the county</returns>
        Task<IEnumerable<SiteConfigurationModel>> GetSitesByCountyAsync(string county);

        /// <summary>
        /// Add or update site configuration
        /// </summary>
        /// <param name="site">Site configuration</param>
        /// <returns>Success indicator</returns>
        Task<bool> SaveSiteAsync(SiteConfigurationModel site);

        /// <summary>
        /// Delete site configuration
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Success indicator</returns>
        Task<bool> DeleteSiteAsync(int siteId);

        #endregion

        #region Sensor Configuration

        /// <summary>
        /// Get all sensor layout configurations
        /// </summary>
        /// <returns>List of all sensor configurations</returns>
        Task<IEnumerable<SensorConfigurationModel>> GetAllSensorsAsync();

        /// <summary>
        /// Get sensor configurations for a specific site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>List of sensor configurations for the site</returns>
        Task<IEnumerable<SensorConfigurationModel>> GetSensorsBySiteAsync(int siteId);

        /// <summary>
        /// Get sensor configuration by site and channel
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <returns>Sensor configuration or null if not found</returns>
        Task<SensorConfigurationModel?> GetSensorAsync(int siteId, string channelId);

        /// <summary>
        /// Add or update sensor configuration
        /// </summary>
        /// <param name="sensor">Sensor configuration</param>
        /// <returns>Success indicator</returns>
        Task<bool> SaveSensorAsync(SensorConfigurationModel sensor);

        /// <summary>
        /// Update sensor layout position
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="x">New X position (0-100%)</param>
        /// <param name="y">New Y position (0-100%)</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateSensorPositionAsync(int siteId, string channelId, double x, double y);

        /// <summary>
        /// Delete sensor configuration
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <returns>Success indicator</returns>
        Task<bool> DeleteSensorAsync(int siteId, string channelId);

        #endregion

        #region County Configuration

        /// <summary>
        /// Get all county configurations
        /// </summary>
        /// <returns>List of all county configurations</returns>
        Task<IEnumerable<CountyConfigurationModel>> GetAllCountiesAsync();

        /// <summary>
        /// Get county configuration by name
        /// </summary>
        /// <param name="countyName">County name</param>
        /// <returns>County configuration or null if not found</returns>
        Task<CountyConfigurationModel?> GetCountyAsync(string countyName);

        /// <summary>
        /// Add or update county configuration
        /// </summary>
        /// <param name="county">County configuration</param>
        /// <returns>Success indicator</returns>
        Task<bool> SaveCountyAsync(CountyConfigurationModel county);

        #endregion

        #region Data Conversion

        /// <summary>
        /// Convert site configuration to SiteInfo (for repository compatibility)
        /// </summary>
        /// <param name="siteConfig">Site configuration</param>
        /// <returns>SiteInfo object</returns>
        SiteInfo ConvertToSiteInfo(SiteConfigurationModel siteConfig);

        /// <summary>
        /// Convert multiple site configurations to SiteInfo list
        /// </summary>
        /// <param name="siteConfigs">Site configurations</param>
        /// <returns>List of SiteInfo objects</returns>
        IEnumerable<SiteInfo> ConvertToSiteInfoList(IEnumerable<SiteConfigurationModel> siteConfigs);

        #endregion

        #region Configuration Management

        /// <summary>
        /// Reload configuration from files
        /// </summary>
        /// <returns>Success indicator</returns>
        Task<bool> ReloadConfigurationAsync();

        /// <summary>
        /// Save all configuration changes to files
        /// </summary>
        /// <returns>Success indicator</returns>
        Task<bool> SaveAllConfigurationAsync();

        /// <summary>
        /// Validate all configuration data
        /// </summary>
        /// <returns>Validation results</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationAsync();

        /// <summary>
        /// Get configuration file paths
        /// </summary>
        /// <returns>Configuration file paths</returns>
        ConfigurationPaths GetConfigurationPaths();

        #endregion
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int SiteCount { get; set; }
        public int SensorCount { get; set; }
        public int CountyCount { get; set; }
    }

    /// <summary>
    /// Configuration file paths
    /// </summary>
    public class ConfigurationPaths
    {
        public string SitesPath { get; set; } = "";
        public string SensorsPath { get; set; } = "";
        public string CountiesPath { get; set; } = "";
        public string LayoutsPath { get; set; } = "";
    }
}