// File: Repositories/Interfaces/ISensorRepository.cs
// Repository interface for sensor data access operations

using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for sensor data operations
    /// Abstracts all database operations for sensors
    /// </summary>
    public interface ISensorRepository
    {
        /// <summary>
        /// Get all sensors from all sites
        /// </summary>
        /// <returns>List of all sensors</returns>
        Task<IEnumerable<Sensor>> GetAllAsync();

        /// <summary>
        /// Get all sensors for a specific site
        /// </summary>
        /// <param name="siteId">Site ID to filter by</param>
        /// <returns>List of sensors for the specified site</returns>
        Task<IEnumerable<Sensor>> GetBySiteIdAsync(int siteId);

        /// <summary>
        /// Get a specific sensor by its ID
        /// </summary>
        /// <param name="id">Sensor ID</param>
        /// <returns>Sensor or null if not found</returns>
        Task<Sensor?> GetByIdAsync(int id);

        /// <summary>
        /// Get all sensors currently in alarm state (Status = 1 or 2)
        /// Status meanings: 1=Alarm Level 1, 2=Alarm Level 2, 3=Detector Error, 4=Disabled, 5=Line Open Fault, 6=Line Short Fault
        /// </summary>
        /// <returns>List of sensors in alarm (status 1 or 2)</returns>
        Task<IEnumerable<Sensor>> GetSensorsInAlarmAsync();

        /// <summary>
        /// Create a new sensor record
        /// </summary>
        /// <param name="sensor">Sensor entity to create</param>
        /// <returns>Created sensor with ID assigned</returns>
        Task<Sensor> CreateAsync(Sensor sensor);

        /// <summary>
        /// Update an existing sensor record
        /// </summary>
        /// <param name="sensor">Sensor entity with updated values</param>
        /// <returns>Updated sensor</returns>
        Task<Sensor> UpdateAsync(Sensor sensor);

        /// <summary>
        /// Delete a sensor record
        /// </summary>
        /// <param name="id">Sensor ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Count total sensors for a specific site
        /// </summary>
        /// <param name="siteId">Site ID to count sensors for</param>
        /// <returns>Number of sensors at the site</returns>
        Task<int> CountBySiteIdAsync(int siteId);

        /// <summary>
        /// Count sensors by status for a specific site
        /// Pure data access - no business logic
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="status">Status to count</param>
        /// <returns>Number of sensors with specified status</returns>
        Task<int> CountByStatusAsync(int siteId, int status);

        /// <summary>
        /// Get the last update time for all sensors at a site
        /// Pure data access - used by service layer to determine connectivity
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Most recent LastUpdated timestamp, or null if no sensors</returns>
        Task<DateTime?> GetLastUpdateTimeAsync(int siteId);
    }
}