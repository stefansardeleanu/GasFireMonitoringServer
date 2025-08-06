// File: Repositories/Interfaces/IAlarmRepository.cs
// Repository interface for alarm data access operations

using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for alarm data operations
    /// Abstracts all database operations for alarms
    /// </summary>
    public interface IAlarmRepository
    {
        /// <summary>
        /// Get all alarms with optional filtering
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>Filtered list of alarms</returns>
        Task<IEnumerable<Alarm>> GetAllAsync(int? siteId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

        /// <summary>
        /// Get alarms for a specific site
        /// </summary>
        /// <param name="siteId">Site ID to filter by</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of alarms for the specified site</returns>
        Task<IEnumerable<Alarm>> GetBySiteIdAsync(int siteId, int limit = 50);

        /// <summary>
        /// Get currently active alarms (last 24 hours)
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <returns>List of recent alarms</returns>
        Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(int? siteId = null);

        /// <summary>
        /// Get alarms for a specific sensor
        /// </summary>
        /// <param name="sensorTag">Sensor tag to filter by</param>
        /// <param name="days">Number of days to look back</param>
        /// <returns>List of alarms for the sensor</returns>
        Task<IEnumerable<Alarm>> GetBySensorTagAsync(string sensorTag, int days = 7);

        /// <summary>
        /// Create a new alarm record
        /// </summary>
        /// <param name="alarm">Alarm entity to create</param>
        /// <returns>Created alarm with ID assigned</returns>
        Task<Alarm> CreateAsync(Alarm alarm);

        /// <summary>
        /// Update an existing alarm record
        /// </summary>
        /// <param name="alarm">Alarm entity with updated values</param>
        /// <returns>Updated alarm</returns>
        Task<Alarm> UpdateAsync(Alarm alarm);

        /// <summary>
        /// Delete an alarm record
        /// </summary>
        /// <param name="id">Alarm ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Count total alarms for statistics
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="days">Number of days to count (default 30)</param>
        /// <returns>Number of alarms in the time period</returns>
        Task<int> CountAsync(int? siteId = null, int days = 30);

        /// <summary>
        /// Count alarms by day for trend analysis
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <param name="days">Number of days to analyze (default 7)</param>
        /// <returns>Dictionary with date as key and alarm count as value</returns>
        Task<Dictionary<DateTime, int>> GetAlarmCountsByDayAsync(int? siteId = null, int days = 7);

        /// <summary>
        /// Get most frequent alarm types for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="days">Number of days to analyze (default 30)</param>
        /// <returns>Dictionary with alarm message as key and count as value</returns>
        Task<Dictionary<string, int>> GetAlarmTypeFrequencyAsync(int siteId, int days = 30);

        /// <summary>
        /// Get alarm statistics for dashboard
        /// </summary>
        /// <param name="siteId">Optional site ID filter</param>
        /// <returns>Alarm statistics object</returns>
        Task<object> GetAlarmStatisticsAsync(int? siteId = null);
    }
}