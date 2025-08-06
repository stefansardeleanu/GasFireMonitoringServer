// File: Services/Business/Interfaces/IAlarmService.cs
// Business logic interface for alarm operations

using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Alarm statistics for dashboard
    /// </summary>
    public class AlarmStats
    {
        public int TotalAlarms { get; set; }
        public int Last24Hours { get; set; }
        public int Last7Days { get; set; }
        public int Last30Days { get; set; }
        public DateTime? OldestAlarm { get; set; }
        public DateTime? NewestAlarm { get; set; }
        public Dictionary<string, int> AlarmTypeFrequency { get; set; } = new();
        public Dictionary<DateTime, int> DailyTrends { get; set; } = new();
        public double AlarmsPerDay { get; set; }
        public List<string> MostFrequentAlarmTypes { get; set; } = new();
    }

    /// <summary>
    /// Alarm filter parameters for queries
    /// </summary>
    public class AlarmFilter
    {
        public int? SiteId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SensorTag { get; set; }
        public int Limit { get; set; } = 100;
        public bool ActiveOnly { get; set; } = false; // Last 24 hours only
    }

    /// <summary>
    /// Business logic service for alarm operations
    /// Coordinates between repository layer and controllers
    /// </summary>
    public interface IAlarmService
    {
        /// <summary>
        /// Get alarms with business filtering and context
        /// </summary>
        /// <param name="filter">Alarm filter parameters</param>
        /// <returns>Filtered alarms with business context</returns>
        Task<IEnumerable<Alarm>> GetAlarmsAsync(AlarmFilter filter);

        /// <summary>
        /// Get alarms for a specific site with business logic
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="limit">Maximum number of alarms to return</param>
        /// <returns>Site alarms with additional context</returns>
        Task<IEnumerable<Alarm>> GetAlarmsBySiteAsync(int siteId, int limit = 50);

        /// <summary>
        /// Get currently active alarms (last 24 hours)
        /// With business priority and context
        /// </summary>
        /// <param name="siteId">Optional site filter</param>
        /// <returns>Active alarms with priority information</returns>
        Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(int? siteId = null);

        /// <summary>
        /// Get comprehensive alarm statistics
        /// Includes trends and analysis
        /// </summary>
        /// <param name="siteId">Optional site filter</param>
        /// <returns>Complete alarm statistics with business analysis</returns>
        Task<AlarmStats> GetAlarmStatsAsync(int? siteId = null);

        /// <summary>
        /// Create a new alarm with business validation
        /// Used by MQTT data processing
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="siteName">Site name</param>
        /// <param name="sensorTag">Sensor tag</param>
        /// <param name="alarmMessage">Alarm message</param>
        /// <param name="rawMessage">Original MQTT message</param>
        /// <returns>Created alarm</returns>
        Task<Alarm> CreateAlarmAsync(int siteId, string siteName, string sensorTag, string alarmMessage, string rawMessage);

        /// <summary>
        /// Get alarm history for a specific sensor
        /// With business context about alarm patterns
        /// </summary>
        /// <param name="sensorTag">Sensor tag</param>
        /// <param name="days">Number of days to analyze</param>
        /// <returns>Sensor alarm history with patterns</returns>
        Task<IEnumerable<Alarm>> GetSensorAlarmHistoryAsync(string sensorTag, int days = 7);

        /// <summary>
        /// Get alarm trend analysis for dashboard
        /// Business intelligence about alarm patterns
        /// </summary>
        /// <param name="siteId">Optional site filter</param>
        /// <param name="days">Number of days to analyze</param>
        /// <returns>Alarm trend analysis</returns>
        Task<Dictionary<DateTime, int>> GetAlarmTrendsAsync(int? siteId = null, int days = 7);

        /// <summary>
        /// Get most problematic sensors (frequent alarms)
        /// Business analysis for maintenance planning
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="days">Days to analyze</param>
        /// <param name="limit">Number of sensors to return</param>
        /// <returns>Sensors with most frequent alarms</returns>
        Task<IEnumerable<object>> GetProblematicSensorsAsync(int siteId, int days = 30, int limit = 10);

        /// <summary>
        /// Determine alarm severity based on business rules
        /// </summary>
        /// <param name="alarmMessage">Alarm message</param>
        /// <returns>Severity level: "Low", "Medium", "High", "Critical"</returns>
        string DetermineAlarmSeverity(string alarmMessage);

        /// <summary>
        /// Check if there are critical alarms requiring immediate attention
        /// Business rule for dashboard alerts
        /// </summary>
        /// <param name="siteId">Optional site filter</param>
        /// <returns>True if critical alarms exist</returns>
        Task<bool> HasCriticalAlarmsAsync(int? siteId = null);
    }
}