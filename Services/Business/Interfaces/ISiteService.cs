// File: Services/Business/Interfaces/ISiteService.cs
// Business logic interface for site operations

using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Site status breakdown for LED indicators
    /// Shows counts of sensors in each status category
    /// </summary>
    public class SiteStatusBreakdown
    {
        public int NormalCount { get; set; }    // Green LED - sensors with status 0
        public int AlarmCount { get; set; }     // Red LED - sensors with status 1,2
        public int FaultCount { get; set; }     // Orange LED - sensors with status 3,5,6
        public int DisabledCount { get; set; }  // Gray LED - sensors with status 4
        public bool IsOnline { get; set; }      // Overall site connectivity
        public DateTime? LastUpdate { get; set; }
    }

    /// <summary>
    /// County status breakdown for LED indicators
    /// Shows counts of sites in each status category
    /// </summary>
    public class CountyStatusBreakdown
    {
        public int NormalCount { get; set; }    // Green LED - sites with normal status
        public int AlarmCount { get; set; }     // Red LED - sites with alarm status
        public int FaultCount { get; set; }     // Orange LED - sites with fault status
        public int DisabledCount { get; set; }  // Gray LED - sites with disabled status
        public int OfflineCount { get; set; }   // Additional indicator for offline sites
    }

    /// <summary>
    /// Site with status and sensor statistics
    /// Business model combining site info with real-time status
    /// </summary>
    public class SiteWithStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string County { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string OverallStatus { get; set; } = "offline"; // For backward compatibility
        public SiteStatusBreakdown StatusBreakdown { get; set; } = new();
        public int TotalSensors { get; set; }
        public DateTime? LastUpdate { get; set; }
        public bool IsOnline { get; set; }
        public int RecentAlarms { get; set; } // Last 24 hours
    }

    /// <summary>
    /// County grouping with status aggregation
    /// Business model for county-level dashboard
    /// </summary>
    public class CountyGroup
    {
        public string CountyName { get; set; } = "";
        public List<SiteWithStatus> Sites { get; set; } = new();
        public CountyStatusBreakdown StatusBreakdown { get; set; } = new();
        public int TotalSites { get; set; }
        public int OnlineSites { get; set; }
        public int OfflineSites { get; set; }
    }

    /// <summary>
    /// Overall system status summary
    /// Business model for main dashboard
    /// </summary>
    public class StatusSummary
    {
        public int TotalSites { get; set; }
        public int ActiveSites { get; set; }
        public int OfflineSites { get; set; }
        public int SitesWithAlarms { get; set; }
        public int SitesWithFaults { get; set; }
        public int SitesDisabled { get; set; }
        public int TotalSensors { get; set; }
        public int SensorsInAlarm { get; set; }
        public int SensorsWithFaults { get; set; }
        public int SensorsDisabled { get; set; }
        public DateTime LastSystemUpdate { get; set; }
        public double SystemHealthPercentage { get; set; }
    }

    /// <summary>
    /// Detailed site information with sensors and alarms
    /// Business model for site detail view
    /// </summary>
    public class SiteDetail
    {
        public SiteInfo SiteInfo { get; set; } = new();
        public string Status { get; set; } = "offline";
        public IEnumerable<Sensor> Sensors { get; set; } = new List<Sensor>();
        public IEnumerable<Alarm> RecentAlarms { get; set; } = new List<Alarm>();
        public SensorStats SensorStatistics { get; set; } = new();
        public bool IsOnline { get; set; }
        public DateTime? LastUpdate { get; set; }
    }

    /// <summary>
    /// Business logic service for site operations
    /// Handles complex site status calculations and aggregations
    /// </summary>
    public interface ISiteService
    {
        /// <summary>
        /// Get all sites with real-time status and statistics
        /// Combines site info with sensor data for dashboard
        /// </summary>
        /// <returns>All sites with current status information</returns>
        Task<IEnumerable<SiteWithStatus>> GetAllSitesAsync();

        /// <summary>
        /// Get sites grouped by county with aggregated statistics
        /// Business logic for county-level dashboard
        /// </summary>
        /// <returns>County groups with site statistics</returns>
        Task<IEnumerable<CountyGroup>> GetSitesByCountyAsync();

        /// <summary>
        /// Get detailed information for a specific site
        /// Includes sensors, alarms, and comprehensive statistics
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>Complete site details or null if not found</returns>
        Task<SiteDetail?> GetSiteByIdAsync(int id);

        /// <summary>
        /// Get overall system status summary
        /// Business logic for main dashboard
        /// </summary>
        /// <returns>System-wide status and health metrics</returns>
        Task<StatusSummary> GetStatusSummaryAsync();

        /// <summary>
        /// Get site status breakdown for LED indicators
        /// Returns counts for each status category (not single status)
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Status breakdown with counts for each LED indicator</returns>
        Task<SiteStatusBreakdown> GetSiteStatusBreakdownAsync(int siteId);

        /// <summary>
        /// Get county status breakdown for LED indicators  
        /// Returns counts of sites in each status category
        /// </summary>
        /// <param name="county">County name</param>
        /// <returns>County status breakdown with site counts</returns>
        Task<CountyStatusBreakdown> GetCountyStatusBreakdownAsync(string county);

        /// <summary>
        /// Determine primary site status for backward compatibility
        /// Business rule: highest priority status wins
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Primary site status: "normal", "alarm", "fault", "disabled", or "offline"</returns>
        Task<string> GetSiteStatusAsync(int siteId);

        /// <summary>
        /// Update site status (for future use)
        /// Business logic for manual status overrides
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="status">New status</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateSiteStatusAsync(int siteId, string status);

        /// <summary>
        /// Get sites that require attention
        /// Business logic for prioritizing maintenance
        /// </summary>
        /// <returns>Sites with alarms, faults, or offline status</returns>
        Task<IEnumerable<SiteWithStatus>> GetSitesRequiringAttentionAsync();

        /// <summary>
        /// Calculate system health percentage
        /// Business metric: percentage of sites in normal status
        /// </summary>
        /// <returns>System health percentage (0-100)</returns>
        Task<double> GetSystemHealthPercentageAsync();

        /// <summary>
        /// Get basic site information (pass-through to repository)
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>Basic site info</returns>
        Task<SiteInfo?> GetSiteInfoAsync(int id);

        /// <summary>
        /// Check if a site exists (pass-through to repository)
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>True if site exists</returns>
        Task<bool> SiteExistsAsync(int id);
    }
}