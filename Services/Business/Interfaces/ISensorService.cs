// File: Services/Business/Interfaces/ISensorService.cs
// Business logic interface for sensor operations

using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Sensor statistics for a site
    /// </summary>
    public class SensorStats
    {
        public int TotalSensors { get; set; }
        public int NormalSensors { get; set; }    // Status = 0
        public int AlarmSensors { get; set; }     // Status = 1, 2
        public int FaultSensors { get; set; }     // Status = 3, 5, 6
        public int DisabledSensors { get; set; }  // Status = 4
        public int OnlineSensors { get; set; }    // Updated within last 5 minutes
        public int OfflineSensors { get; set; }   // Not updated recently
        public DateTime? LastUpdate { get; set; }
        public Dictionary<int, int> SensorTypeBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Business logic service for sensor operations
    /// Coordinates between repository layer and controllers
    /// </summary>
    public interface ISensorService
    {
        /// <summary>
        /// Get all sensors with business logic applied
        /// </summary>
        /// <returns>All sensors with computed properties</returns>
        Task<IEnumerable<Sensor>> GetAllSensorsAsync();

        /// <summary>
        /// Get sensors for a specific site with business logic
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Site sensors with status interpretation</returns>
        Task<IEnumerable<Sensor>> GetSensorsBySiteAsync(int siteId);

        /// <summary>
        /// Get a specific sensor by ID
        /// </summary>
        /// <param name="id">Sensor ID</param>
        /// <returns>Sensor with business logic applied</returns>
        Task<Sensor?> GetSensorByIdAsync(int id);

        /// <summary>
        /// Get sensors currently in alarm state (Status = 1 or 2)
        /// Returns sensors with business context
        /// </summary>
        /// <returns>Alarmed sensors with additional context</returns>
        Task<IEnumerable<Sensor>> GetAlarmedSensorsAsync();

        /// <summary>
        /// Get comprehensive sensor statistics for a site
        /// Includes status breakdown and connectivity info
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Complete sensor statistics</returns>
        Task<SensorStats> GetSensorStatsAsync(int siteId);

        /// <summary>
        /// Process a sensor update from MQTT
        /// Handles business rules for sensor updates
        /// </summary>
        /// <param name="sensor">Sensor data from MQTT</param>
        /// <returns>Updated sensor entity</returns>
        Task<Sensor> ProcessSensorUpdateAsync(Sensor sensor);

        /// <summary>
        /// Get sensor type breakdown for all sites
        /// Business logic for dashboard display
        /// </summary>
        /// <returns>Dictionary with site ID as key, sensor type counts as value</returns>
        Task<Dictionary<int, Dictionary<int, int>>> GetSensorTypeBreakdownAsync();

        /// <summary>
        /// Determine if sensors at a site are online
        /// Business rule: online = updated within last 5 minutes
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>True if site has recent sensor updates</returns>
        Task<bool> IsSiteOnlineAsync(int siteId);

        /// <summary>
        /// Get sensors with specific status
        /// </summary>
        /// <param name="status">Status code (0=Normal, 1-2=Alarm, 3,5,6=Fault, 4=Disabled)</param>
        /// <returns>Sensors with specified status</returns>
        Task<IEnumerable<Sensor>> GetSensorsByStatusAsync(int status);

        /// <summary>
        /// Get overall system sensor statistics
        /// For main dashboard display
        /// </summary>
        /// <returns>System-wide sensor statistics</returns>
        Task<SensorStats> GetSystemSensorStatsAsync();
    }
}