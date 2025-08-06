// File: Services/Business/SiteService.cs
// Business logic implementation for site operations

using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Business logic service for site operations
    /// Handles complex site status calculations and aggregations
    /// Combines data from multiple repositories
    /// </summary>
    public class SiteService : ISiteService
    {
        private readonly ISiteRepository _siteRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ISensorService _sensorService;
        private readonly ILogger<SiteService> _logger;

        public SiteService(
            ISiteRepository siteRepository,
            ISensorRepository sensorRepository,
            IAlarmRepository alarmRepository,
            ISensorService sensorService,
            ILogger<SiteService> logger)
        {
            _siteRepository = siteRepository;
            _sensorRepository = sensorRepository;
            _alarmRepository = alarmRepository;
            _sensorService = sensorService;
            _logger = logger;
        }

        /// <summary>
        /// Get all sites with real-time status and statistics
        /// </summary>
        public async Task<IEnumerable<SiteWithStatus>> GetAllSitesAsync()
        {
            try
            {
                _logger.LogDebug("Getting all sites with status and statistics");

                var sites = await _siteRepository.GetAllSiteInfoAsync();
                var sitesWithStatus = new List<SiteWithStatus>();

                foreach (var site in sites)
                {
                    var siteWithStatus = await CreateSiteWithStatusAsync(site);
                    sitesWithStatus.Add(siteWithStatus);
                }

                _logger.LogInformation("Retrieved {SiteCount} sites with status information", sitesWithStatus.Count);
                return sitesWithStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sites with status");
                throw;
            }
        }

        /// <summary>
        /// Get sites grouped by county with aggregated statistics
        /// </summary>
        public async Task<IEnumerable<CountyGroup>> GetSitesByCountyAsync()
        {
            try
            {
                _logger.LogDebug("Getting sites grouped by county with status breakdowns");

                var sitesWithStatus = await GetAllSitesAsync();

                var countyGroups = sitesWithStatus
                    .GroupBy(s => s.County)
                    .Select(g => new CountyGroup
                    {
                        CountyName = g.Key,
                        Sites = g.ToList(),
                        TotalSites = g.Count(),
                        OnlineSites = g.Count(s => s.IsOnline),
                        OfflineSites = g.Count(s => !s.IsOnline),
                        StatusBreakdown = new CountyStatusBreakdown
                        {
                            NormalCount = g.Count(s => s.OverallStatus == "normal"),
                            AlarmCount = g.Count(s => s.OverallStatus == "alarm"),
                            FaultCount = g.Count(s => s.OverallStatus == "fault"),
                            DisabledCount = g.Count(s => s.OverallStatus == "disabled"),
                            OfflineCount = g.Count(s => s.OverallStatus == "offline")
                        }
                    })
                    .OrderBy(g => g.CountyName)
                    .ToList();

                _logger.LogInformation("Created county groups with status breakdowns: {CountyCount} counties", countyGroups.Count);
                return countyGroups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sites by county with status breakdowns");
                throw;
            }
        }

        /// <summary>
        /// Get detailed information for a specific site
        /// </summary>
        public async Task<SiteDetail?> GetSiteByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting detailed information for site {SiteId}", id);

                var siteInfo = await _siteRepository.GetSiteInfoAsync(id);
                if (siteInfo == null)
                {
                    _logger.LogWarning("Site {SiteId} not found", id);
                    return null;
                }

                // Get sensors and statistics
                var sensors = await _sensorRepository.GetBySiteIdAsync(id);
                var sensorStats = await _sensorService.GetSensorStatsAsync(id);

                // Get recent alarms (last 10)
                var recentAlarms = await _alarmRepository.GetBySiteIdAsync(id, 10);

                // Determine site status and connectivity
                var status = await GetSiteStatusAsync(id);
                var isOnline = await _sensorService.IsSiteOnlineAsync(id);
                var lastUpdate = await _sensorRepository.GetLastUpdateTimeAsync(id);

                var siteDetail = new SiteDetail
                {
                    SiteInfo = siteInfo,
                    Status = status,
                    Sensors = sensors,
                    RecentAlarms = recentAlarms,
                    SensorStatistics = sensorStats,
                    IsOnline = isOnline,
                    LastUpdate = lastUpdate
                };

                _logger.LogInformation("Retrieved details for site {SiteId}: {Status} status, {SensorCount} sensors",
                    id, status, sensors.Count());

                return siteDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site details for {SiteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get overall system status summary
        /// </summary>
        public async Task<StatusSummary> GetStatusSummaryAsync()
        {
            try
            {
                _logger.LogDebug("Calculating system status summary");

                var sitesWithStatus = await GetAllSitesAsync();
                var sitesList = sitesWithStatus.ToList();

                var systemSensorStats = await _sensorService.GetSystemSensorStatsAsync();
                var systemHealthPercentage = await GetSystemHealthPercentageAsync();

                var summary = new StatusSummary
                {
                    TotalSites = sitesList.Count,
                    ActiveSites = sitesList.Count(s => s.IsOnline),
                    OfflineSites = sitesList.Count(s => !s.IsOnline),
                    SitesWithAlarms = sitesList.Count(s => s.OverallStatus == "alarm"),
                    SitesWithFaults = sitesList.Count(s => s.OverallStatus == "fault"),
                    SitesDisabled = sitesList.Count(s => s.OverallStatus == "disabled"),
                    TotalSensors = systemSensorStats.TotalSensors,
                    SensorsInAlarm = systemSensorStats.AlarmSensors,
                    SensorsWithFaults = systemSensorStats.FaultSensors,
                    SensorsDisabled = systemSensorStats.DisabledSensors,
                    LastSystemUpdate = systemSensorStats.LastUpdate ?? DateTime.MinValue,
                    SystemHealthPercentage = systemHealthPercentage
                };

                _logger.LogInformation("System summary: {TotalSites} sites, {ActiveSites} active, {HealthPercentage}% health",
                    summary.TotalSites, summary.ActiveSites, summary.SystemHealthPercentage);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system status summary");
                throw;
            }
        }

        /// <summary>
        /// Get site status breakdown for LED indicators
        /// Returns counts for each status category (not single status)
        /// </summary>
        public async Task<SiteStatusBreakdown> GetSiteStatusBreakdownAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting status breakdown for site {SiteId}", siteId);

                var sensors = await _sensorRepository.GetBySiteIdAsync(siteId);
                var sensorsList = sensors.ToList();
                var isOnline = await _sensorService.IsSiteOnlineAsync(siteId);
                var lastUpdate = await _sensorRepository.GetLastUpdateTimeAsync(siteId);

                var breakdown = new SiteStatusBreakdown
                {
                    NormalCount = sensorsList.Count(s => s.Status == 0),
                    AlarmCount = sensorsList.Count(s => s.Status == 1 || s.Status == 2),
                    FaultCount = sensorsList.Count(s => s.Status == 3 || s.Status == 5 || s.Status == 6),
                    DisabledCount = sensorsList.Count(s => s.Status == 4),
                    IsOnline = isOnline,
                    LastUpdate = lastUpdate
                };

                _logger.LogDebug("Site {SiteId} breakdown: Normal={Normal}, Alarm={Alarm}, Fault={Fault}, Disabled={Disabled}",
                    siteId, breakdown.NormalCount, breakdown.AlarmCount, breakdown.FaultCount, breakdown.DisabledCount);

                return breakdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status breakdown for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get county status breakdown for LED indicators
        /// Returns counts of sites in each status category
        /// </summary>
        public async Task<CountyStatusBreakdown> GetCountyStatusBreakdownAsync(string county)
        {
            try
            {
                _logger.LogDebug("Getting status breakdown for county {County}", county);

                // Get all sites in the county
                var countySites = await _siteRepository.GetSitesByCountyNameAsync(county);
                var breakdown = new CountyStatusBreakdown();

                // Calculate status for each site and aggregate
                foreach (var site in countySites)
                {
                    var primaryStatus = await GetSiteStatusAsync(site.Id);

                    switch (primaryStatus)
                    {
                        case "normal":
                            breakdown.NormalCount++;
                            break;
                        case "alarm":
                            breakdown.AlarmCount++;
                            break;
                        case "fault":
                            breakdown.FaultCount++;
                            break;
                        case "disabled":
                            breakdown.DisabledCount++;
                            break;
                        case "offline":
                            breakdown.OfflineCount++;
                            break;
                    }
                }

                _logger.LogDebug("County {County} breakdown: Normal={Normal}, Alarm={Alarm}, Fault={Fault}, Disabled={Disabled}, Offline={Offline}",
                    county, breakdown.NormalCount, breakdown.AlarmCount, breakdown.FaultCount, breakdown.DisabledCount, breakdown.OfflineCount);

                return breakdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status breakdown for county {County}", county);
                throw;
            }
        }

        /// <summary>
        /// Determine site status based on sensor data
        /// Business rule: highest priority status wins (for backward compatibility)
        /// </summary>
        public async Task<string> GetSiteStatusAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Determining primary status for site {SiteId}", siteId);

                var breakdown = await GetSiteStatusBreakdownAsync(siteId);

                // Priority logic: return the highest priority status that has count > 0
                if (breakdown.AlarmCount > 0)
                    return "alarm";

                if (breakdown.FaultCount > 0)
                    return "fault";

                if (breakdown.DisabledCount > 0 && breakdown.NormalCount == 0)
                    return "disabled"; // Only if ALL sensors are disabled

                if (!breakdown.IsOnline)
                    return "offline";

                return "normal";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining primary status for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Update site status (for future use)
        /// </summary>
        public async Task<bool> UpdateSiteStatusAsync(int siteId, string status)
        {
            try
            {
                _logger.LogInformation("Manual status update for site {SiteId} to {Status}", siteId, status);

                // Validate status
                var validStatuses = new[] { "normal", "alarm", "fault", "disabled", "offline" };
                if (!validStatuses.Contains(status.ToLowerInvariant()))
                {
                    _logger.LogWarning("Invalid status {Status} for site {SiteId}", status, siteId);
                    return false;
                }

                // For now, just log the request - actual implementation would depend on requirements
                // This might involve updating a site status override table in the future

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get sites that require attention
        /// </summary>
        public async Task<IEnumerable<SiteWithStatus>> GetSitesRequiringAttentionAsync()
        {
            try
            {
                _logger.LogDebug("Getting sites requiring attention");

                var allSites = await GetAllSitesAsync();

                var sitesNeedingAttention = allSites
                    .Where(s => s.OverallStatus == "alarm" || s.OverallStatus == "fault" || s.OverallStatus == "offline")
                    .OrderBy(s => GetStatusPriority(s.OverallStatus))
                    .ThenByDescending(s => s.RecentAlarms)
                    .ToList();

                _logger.LogInformation("Found {Count} sites requiring attention", sitesNeedingAttention.Count);
                return sitesNeedingAttention;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sites requiring attention");
                throw;
            }
        }

        /// <summary>
        /// Calculate system health percentage
        /// </summary>
        public async Task<double> GetSystemHealthPercentageAsync()
        {
            try
            {
                _logger.LogDebug("Calculating system health percentage");

                var allSites = await GetAllSitesAsync();
                var sitesList = allSites.ToList();

                if (!sitesList.Any())
                {
                    return 0.0;
                }

                var normalSites = sitesList.Count(s => s.OverallStatus == "normal");
                var healthPercentage = Math.Round((double)normalSites / sitesList.Count * 100, 1);

                _logger.LogDebug("System health: {NormalSites}/{TotalSites} = {HealthPercentage}%",
                    normalSites, sitesList.Count, healthPercentage);

                return healthPercentage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system health percentage");
                throw;
            }
        }

        /// <summary>
        /// Get basic site information (pass-through to repository)
        /// </summary>
        public async Task<SiteInfo?> GetSiteInfoAsync(int id)
        {
            try
            {
                return await _siteRepository.GetSiteInfoAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site info for {SiteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Check if a site exists (pass-through to repository)
        /// </summary>
        public async Task<bool> SiteExistsAsync(int id)
        {
            try
            {
                return await _siteRepository.SiteExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if site {SiteId} exists", id);
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Create a SiteWithStatus object for a site
        /// Combines site info with real-time sensor and alarm data
        /// </summary>
        private async Task<SiteWithStatus> CreateSiteWithStatusAsync(SiteInfo site)
        {
            try
            {
                // Get status breakdown for LED indicators
                var statusBreakdown = await GetSiteStatusBreakdownAsync(site.Id);

                // Get primary status for backward compatibility
                var primaryStatus = await GetSiteStatusAsync(site.Id);

                // Get recent alarms count (last 24 hours)
                var recentAlarmsCount = await _alarmRepository.CountAsync(site.Id, 1);

                return new SiteWithStatus
                {
                    Id = site.Id,
                    Name = site.Name,
                    County = site.County,
                    Latitude = site.Latitude,
                    Longitude = site.Longitude,
                    OverallStatus = primaryStatus,
                    StatusBreakdown = statusBreakdown,
                    TotalSensors = statusBreakdown.NormalCount + statusBreakdown.AlarmCount +
                                  statusBreakdown.FaultCount + statusBreakdown.DisabledCount,
                    LastUpdate = statusBreakdown.LastUpdate,
                    IsOnline = statusBreakdown.IsOnline,
                    RecentAlarms = recentAlarmsCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SiteWithStatus for site {SiteId}", site.Id);

                // Return a basic site with error status on failure
                return new SiteWithStatus
                {
                    Id = site.Id,
                    Name = site.Name,
                    County = site.County,
                    Latitude = site.Latitude,
                    Longitude = site.Longitude,
                    OverallStatus = "offline",
                    StatusBreakdown = new SiteStatusBreakdown(),
                    IsOnline = false
                };
            }
        }

        /// <summary>
        /// Get status priority for sorting (lower number = higher priority)
        /// Business rule for prioritizing attention
        /// </summary>
        private int GetStatusPriority(string status)
        {
            return status switch
            {
                "alarm" => 1,    // Highest priority
                "fault" => 2,    // Second priority
                "offline" => 3,  // Third priority
                "disabled" => 4, // Fourth priority
                "normal" => 5,   // Lowest priority
                _ => 99          // Unknown status
            };
        }

        #endregion
    }
}