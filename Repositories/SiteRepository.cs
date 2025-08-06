// File: Repositories/SiteRepository.cs
// Concrete implementation of ISiteRepository
// Handles site information from configuration file

using Microsoft.Extensions.Logging;
using System.Text.Json;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Repositories
{
    /// <summary>
    /// Repository implementation for site data operations
    /// Reads site information from JSON configuration file
    /// </summary>
    public class SiteRepository : ISiteRepository
    {
        private readonly ILogger<SiteRepository> _logger;
        private readonly string _configurationPath;
        private List<SiteInfo>? _sites;
        private DateTime _lastFileRead = DateTime.MinValue;

        public SiteRepository(ILogger<SiteRepository> logger)
        {
            _logger = logger;
            // Configuration file path - can be overridden via environment variable
            _configurationPath = Environment.GetEnvironmentVariable("SITES_CONFIG_PATH")
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "Data", "sites.json");
        }

        /// <summary>
        /// Load sites from configuration file with caching
        /// </summary>
        private async Task<List<SiteInfo>> LoadSitesAsync()
        {
            try
            {
                var fileInfo = new FileInfo(_configurationPath);

                // Check if we need to reload the file
                if (_sites == null || !fileInfo.Exists || fileInfo.LastWriteTime > _lastFileRead)
                {
                    _logger.LogDebug("Loading sites from configuration file: {ConfigPath}", _configurationPath);

                    if (!fileInfo.Exists)
                    {
                        _logger.LogWarning("Sites configuration file not found at {ConfigPath}", _configurationPath);
                        return new List<SiteInfo>();
                    }

                    var jsonContent = await File.ReadAllTextAsync(_configurationPath);
                    _sites = JsonSerializer.Deserialize<List<SiteInfo>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SiteInfo>();

                    _lastFileRead = DateTime.UtcNow;
                    _logger.LogInformation("Loaded {SiteCount} sites from configuration file", _sites.Count);
                }

                return _sites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sites from configuration file {ConfigPath}", _configurationPath);
                throw;
            }
        }

        /// <summary>
        /// Get all basic site information
        /// Pure data access - returns raw site data from configuration
        /// </summary>
        public async Task<IEnumerable<SiteInfo>> GetAllSiteInfoAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all site information");

                var sites = await LoadSitesAsync();
                return sites.ToList(); // Return copy to prevent external modification
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all site information");
                throw;
            }
        }

        /// <summary>
        /// Get basic site information by ID
        /// Pure data access - no status calculations
        /// </summary>
        public async Task<SiteInfo?> GetSiteInfoAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving site information for ID {SiteId}", id);

                var sites = await LoadSitesAsync();
                return sites.FirstOrDefault(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site information for ID {SiteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Check if a site exists
        /// Pure data access - simple existence check
        /// </summary>
        public async Task<bool> SiteExistsAsync(int id)
        {
            try
            {
                _logger.LogDebug("Checking if site {SiteId} exists", id);

                var sites = await LoadSitesAsync();
                return sites.Any(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if site {SiteId} exists", id);
                throw;
            }
        }

        /// <summary>
        /// Get sites for a specific county
        /// Pure data access - simple filtering
        /// </summary>
        public async Task<IEnumerable<SiteInfo>> GetSitesByCountyNameAsync(string county)
        {
            try
            {
                _logger.LogDebug("Retrieving sites for county {County}", county);

                var sites = await LoadSitesAsync();
                return sites
                    .Where(s => s.County.Equals(county, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sites for county {County}", county);
                throw;
            }
        }

        /// <summary>
        /// Get all distinct county names
        /// Pure data access - returns unique county list
        /// </summary>
        public async Task<IEnumerable<string>> GetAllCountiesAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all county names");

                var sites = await LoadSitesAsync();
                return sites
                    .Select(s => s.County)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving county names");
                throw;
            }
        }
    }
}