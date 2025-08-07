// File: Services/Business/ConfigurationService.cs
// Configuration service implementation for JSON configuration management

using System.Text.Json;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Models.Configuration;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Configuration service implementation
    /// Manages JSON configuration files and provides caching
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly ConfigurationPaths _paths;

        // In-memory cache for configuration data
        private List<SiteConfigurationModel>? _sites;
        private List<SensorConfigurationModel>? _sensors;
        private List<CountyConfigurationModel>? _counties;

        // Cache timestamp tracking
        private DateTime _sitesLastLoaded = DateTime.MinValue;
        private DateTime _sensorsLastLoaded = DateTime.MinValue;
        private DateTime _countiesLastLoaded = DateTime.MinValue;

        // JSON serializer options
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _paths = InitializeConfigurationPaths();
        }

        #region Site Configuration

        public async Task<IEnumerable<SiteConfigurationModel>> GetAllSitesAsync()
        {
            try
            {
                await EnsureSitesLoadedAsync();
                return _sites?.ToList() ?? new List<SiteConfigurationModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sites");
                throw;
            }
        }

        public async Task<SiteConfigurationModel?> GetSiteAsync(int siteId)
        {
            try
            {
                var sites = await GetAllSitesAsync();
                return sites.FirstOrDefault(s => s.Id == siteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site {SiteId}", siteId);
                throw;
            }
        }

        public async Task<IEnumerable<SiteConfigurationModel>> GetSitesByCountyAsync(string county)
        {
            try
            {
                var sites = await GetAllSitesAsync();
                return sites.Where(s => s.County.Equals(county, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sites for county {County}", county);
                throw;
            }
        }

        public async Task<bool> SaveSiteAsync(SiteConfigurationModel site)
        {
            try
            {
                if (!site.IsValid())
                {
                    _logger.LogWarning("Invalid site configuration for site {SiteId}: {SiteName}", site.Id, site.Name);
                    return false;
                }

                await EnsureSitesLoadedAsync();

                var existingIndex = _sites!.FindIndex(s => s.Id == site.Id);
                if (existingIndex >= 0)
                {
                    _sites[existingIndex] = site;
                    _logger.LogInformation("Updated site configuration for {SiteId}: {SiteName}", site.Id, site.Name);
                }
                else
                {
                    _sites.Add(site);
                    _logger.LogInformation("Added new site configuration for {SiteId}: {SiteName}", site.Id, site.Name);
                }

                return await SaveSitesToFileAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving site {SiteId}", site.Id);
                return false;
            }
        }

        public async Task<bool> DeleteSiteAsync(int siteId)
        {
            try
            {
                await EnsureSitesLoadedAsync();

                var removed = _sites!.RemoveAll(s => s.Id == siteId);
                if (removed > 0)
                {
                    _logger.LogInformation("Deleted site configuration for {SiteId}", siteId);
                    return await SaveSitesToFileAsync();
                }

                _logger.LogWarning("Site {SiteId} not found for deletion", siteId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteId}", siteId);
                return false;
            }
        }

        #endregion

        #region Sensor Configuration

        public async Task<IEnumerable<SensorConfigurationModel>> GetAllSensorsAsync()
        {
            try
            {
                await EnsureSensorsLoadedAsync();
                return _sensors?.ToList() ?? new List<SensorConfigurationModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sensors");
                throw;
            }
        }

        public async Task<IEnumerable<SensorConfigurationModel>> GetSensorsBySiteAsync(int siteId)
        {
            try
            {
                var sensors = await GetAllSensorsAsync();
                return sensors.Where(s => s.SiteId == siteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensors for site {SiteId}", siteId);
                throw;
            }
        }

        public async Task<SensorConfigurationModel?> GetSensorAsync(int siteId, string channelId)
        {
            try
            {
                var sensors = await GetSensorsBySiteAsync(siteId);
                return sensors.FirstOrDefault(s => s.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor {SiteId}/{ChannelId}", siteId, channelId);
                throw;
            }
        }

        public async Task<bool> SaveSensorAsync(SensorConfigurationModel sensor)
        {
            try
            {
                if (!sensor.IsValid())
                {
                    _logger.LogWarning("Invalid sensor configuration for {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);
                    return false;
                }

                await EnsureSensorsLoadedAsync();

                var existingIndex = _sensors!.FindIndex(s => s.SiteId == sensor.SiteId &&
                    s.ChannelId.Equals(sensor.ChannelId, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    _sensors[existingIndex] = sensor;
                    _logger.LogInformation("Updated sensor configuration for {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);
                }
                else
                {
                    _sensors.Add(sensor);
                    _logger.LogInformation("Added new sensor configuration for {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);
                }

                return await SaveSensorsToFileAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sensor {SiteId}/{ChannelId}", sensor.SiteId, sensor.ChannelId);
                return false;
            }
        }

        public async Task<bool> UpdateSensorPositionAsync(int siteId, string channelId, double x, double y)
        {
            try
            {
                if (x < 0 || x > 100 || y < 0 || y > 100)
                {
                    _logger.LogWarning("Invalid sensor position coordinates: {X}, {Y}", x, y);
                    return false;
                }

                var sensor = await GetSensorAsync(siteId, channelId);
                if (sensor == null)
                {
                    // Create new sensor configuration
                    sensor = new SensorConfigurationModel
                    {
                        SiteId = siteId,
                        ChannelId = channelId,
                        LayoutX = x,
                        LayoutY = y
                    };
                }
                else
                {
                    sensor.LayoutX = x;
                    sensor.LayoutY = y;
                }

                return await SaveSensorAsync(sensor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor position for {SiteId}/{ChannelId}", siteId, channelId);
                return false;
            }
        }

        public async Task<bool> DeleteSensorAsync(int siteId, string channelId)
        {
            try
            {
                await EnsureSensorsLoadedAsync();

                var removed = _sensors!.RemoveAll(s => s.SiteId == siteId &&
                    s.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    _logger.LogInformation("Deleted sensor configuration for {SiteId}/{ChannelId}", siteId, channelId);
                    return await SaveSensorsToFileAsync();
                }

                _logger.LogWarning("Sensor {SiteId}/{ChannelId} not found for deletion", siteId, channelId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sensor {SiteId}/{ChannelId}", siteId, channelId);
                return false;
            }
        }

        #endregion

        #region County Configuration

        public async Task<IEnumerable<CountyConfigurationModel>> GetAllCountiesAsync()
        {
            try
            {
                await EnsureCountiesLoadedAsync();
                return _counties?.ToList() ?? new List<CountyConfigurationModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all counties");
                throw;
            }
        }

        public async Task<CountyConfigurationModel?> GetCountyAsync(string countyName)
        {
            try
            {
                var counties = await GetAllCountiesAsync();
                return counties.FirstOrDefault(c => c.Name.Equals(countyName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting county {CountyName}", countyName);
                throw;
            }
        }

        public async Task<bool> SaveCountyAsync(CountyConfigurationModel county)
        {
            try
            {
                if (!county.IsValid())
                {
                    _logger.LogWarning("Invalid county configuration for {CountyName}", county.Name);
                    return false;
                }

                await EnsureCountiesLoadedAsync();

                var existingIndex = _counties!.FindIndex(c => c.Name.Equals(county.Name, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    _counties[existingIndex] = county;
                    _logger.LogInformation("Updated county configuration for {CountyName}", county.Name);
                }
                else
                {
                    _counties.Add(county);
                    _logger.LogInformation("Added new county configuration for {CountyName}", county.Name);
                }

                return await SaveCountiesToFileAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving county {CountyName}", county.Name);
                return false;
            }
        }

        #endregion

        #region Data Conversion

        public SiteInfo ConvertToSiteInfo(SiteConfigurationModel siteConfig)
        {
            return new SiteInfo
            {
                Id = siteConfig.Id,
                Name = siteConfig.Name,
                County = siteConfig.County,
                Latitude = siteConfig.MapY,  // Note: Using MapY as Latitude for display
                Longitude = siteConfig.MapX  // Note: Using MapX as Longitude for display
            };
        }

        public IEnumerable<SiteInfo> ConvertToSiteInfoList(IEnumerable<SiteConfigurationModel> siteConfigs)
        {
            return siteConfigs.Select(ConvertToSiteInfo);
        }

        #endregion

        #region Configuration Management

        public async Task<bool> ReloadConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Reloading configuration from files");

                // Clear cache to force reload
                _sites = null;
                _sensors = null;
                _counties = null;
                _sitesLastLoaded = DateTime.MinValue;
                _sensorsLastLoaded = DateTime.MinValue;
                _countiesLastLoaded = DateTime.MinValue;

                // Load all configurations
                await GetAllSitesAsync();
                await GetAllSensorsAsync();
                await GetAllCountiesAsync();

                _logger.LogInformation("Configuration reloaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration");
                return false;
            }
        }

        public async Task<bool> SaveAllConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Saving all configuration to files");

                var sitesResult = await SaveSitesToFileAsync();
                var sensorsResult = await SaveSensorsToFileAsync();
                var countiesResult = await SaveCountiesToFileAsync();

                var success = sitesResult && sensorsResult && countiesResult;

                if (success)
                    _logger.LogInformation("All configuration saved successfully");
                else
                    _logger.LogWarning("Some configuration files failed to save");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration");
                return false;
            }
        }

        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var result = new ConfigurationValidationResult();

            try
            {
                var sites = await GetAllSitesAsync();
                var sensors = await GetAllSensorsAsync();
                var counties = await GetAllCountiesAsync();

                result.SiteCount = sites.Count();
                result.SensorCount = sensors.Count();
                result.CountyCount = counties.Count();

                // Validate sites
                foreach (var site in sites)
                {
                    if (!site.IsValid())
                        result.Errors.Add($"Invalid site configuration: {site.Name} (ID: {site.Id})");
                }

                // Validate sensors
                foreach (var sensor in sensors)
                {
                    if (!sensor.IsValid())
                        result.Errors.Add($"Invalid sensor configuration: {sensor.SiteId}/{sensor.ChannelId}");

                    // Check if sensor site exists
                    if (!sites.Any(s => s.Id == sensor.SiteId))
                        result.Errors.Add($"Sensor references non-existent site: {sensor.SiteId}/{sensor.ChannelId}");
                }

                // Validate counties
                foreach (var county in counties)
                {
                    if (!county.IsValid())
                        result.Errors.Add($"Invalid county configuration: {county.Name}");

                    // Check if county sites exist
                    foreach (var siteId in county.Sites)
                    {
                        if (!sites.Any(s => s.Id == siteId))
                            result.Warnings.Add($"County {county.Name} references non-existent site: {siteId}");
                    }
                }

                result.IsValid = !result.Errors.Any();

                _logger.LogInformation("Configuration validation completed: {IsValid}, {ErrorCount} errors, {WarningCount} warnings",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration validation");
                result.Errors.Add($"Validation failed with exception: {ex.Message}");
            }

            return result;
        }

        public ConfigurationPaths GetConfigurationPaths()
        {
            return _paths;
        }

        #endregion

        #region Private Helper Methods

        private ConfigurationPaths InitializeConfigurationPaths()
        {
            var basePath = Environment.GetEnvironmentVariable("CONFIG_BASE_PATH")
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "Data");

            return new ConfigurationPaths
            {
                SitesPath = Path.Combine(basePath, "sites.json"),
                SensorsPath = Path.Combine(basePath, "sensors.json"),
                CountiesPath = Path.Combine(basePath, "counties.json"),
                LayoutsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "layouts")
            };
        }

        private async Task EnsureSitesLoadedAsync()
        {
            if (ShouldReloadFile(_paths.SitesPath, _sitesLastLoaded))
            {
                _sites = await LoadJsonFileAsync<List<SiteConfigurationModel>>(_paths.SitesPath)
                    ?? new List<SiteConfigurationModel>();
                _sitesLastLoaded = DateTime.UtcNow;
                _logger.LogDebug("Loaded {Count} sites from configuration", _sites.Count);
            }
        }

        private async Task EnsureSensorsLoadedAsync()
        {
            if (ShouldReloadFile(_paths.SensorsPath, _sensorsLastLoaded))
            {
                _sensors = await LoadJsonFileAsync<List<SensorConfigurationModel>>(_paths.SensorsPath)
                    ?? new List<SensorConfigurationModel>();
                _sensorsLastLoaded = DateTime.UtcNow;
                _logger.LogDebug("Loaded {Count} sensors from configuration", _sensors.Count);
            }
        }

        private async Task EnsureCountiesLoadedAsync()
        {
            if (ShouldReloadFile(_paths.CountiesPath, _countiesLastLoaded))
            {
                _counties = await LoadJsonFileAsync<List<CountyConfigurationModel>>(_paths.CountiesPath)
                    ?? new List<CountyConfigurationModel>();
                _countiesLastLoaded = DateTime.UtcNow;
                _logger.LogDebug("Loaded {Count} counties from configuration", _counties.Count);
            }
        }

        private bool ShouldReloadFile(string filePath, DateTime lastLoaded)
        {
            if (!File.Exists(filePath))
                return false;

            var fileInfo = new FileInfo(filePath);
            return lastLoaded == DateTime.MinValue || fileInfo.LastWriteTime > lastLoaded;
        }

        private async Task<T?> LoadJsonFileAsync<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Configuration file not found: {FilePath}", filePath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration file: {FilePath}", filePath);
                return null;
            }
        }

        private async Task<bool> SaveJsonFileAsync<T>(string filePath, T data) where T : class
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonContent);

                _logger.LogDebug("Saved configuration file: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration file: {FilePath}", filePath);
                return false;
            }
        }

        private async Task<bool> SaveSitesToFileAsync()
        {
            if (_sites == null) return false;
            return await SaveJsonFileAsync(_paths.SitesPath, _sites);
        }

        private async Task<bool> SaveSensorsToFileAsync()
        {
            if (_sensors == null) return false;
            return await SaveJsonFileAsync(_paths.SensorsPath, _sensors);
        }

        private async Task<bool> SaveCountiesToFileAsync()
        {
            if (_counties == null) return false;
            return await SaveJsonFileAsync(_paths.CountiesPath, _counties);
        }

        #endregion
    }
}