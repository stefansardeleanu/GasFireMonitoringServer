// File: Repositories/SiteRepository.cs
// Concrete implementation of ISiteRepository
// Handles site information from configuration service

using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;

namespace GasFireMonitoringServer.Repositories
{
    /// <summary>
    /// Repository implementation for site data operations
    /// Uses ConfigurationService for dynamic site configuration
    /// </summary>
    public class SiteRepository : ISiteRepository
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SiteRepository> _logger;

        public SiteRepository(IConfigurationService configurationService, ILogger<SiteRepository> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all basic site information
        /// Pure data access - returns raw site data from configuration
        /// </summary>
        public async Task<IEnumerable<SiteInfo>> GetAllSiteInfoAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all site information from configuration");

                var siteConfigs = await _configurationService.GetAllSitesAsync();
                return _configurationService.ConvertToSiteInfoList(siteConfigs);
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

                var siteConfig = await _configurationService.GetSiteAsync(id);
                return siteConfig != null ? _configurationService.ConvertToSiteInfo(siteConfig) : null;
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

                var siteConfig = await _configurationService.GetSiteAsync(id);
                return siteConfig != null;
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

                var siteConfigs = await _configurationService.GetSitesByCountyAsync(county);
                return _configurationService.ConvertToSiteInfoList(siteConfigs);
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

                var countyConfigs = await _configurationService.GetAllCountiesAsync();
                return countyConfigs.Select(c => c.Name).OrderBy(name => name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving county names");
                throw;
            }
        }
    }
}