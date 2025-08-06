// File: Repositories/Interfaces/ISiteRepository.cs
// Repository interface for site data access operations

namespace GasFireMonitoringServer.Repositories.Interfaces
{
    /// <summary>
    /// Site information model
    /// Represents site data from configuration file
    /// </summary>
    public class SiteInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string County { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Repository interface for site data operations
    /// Pure data access - business logic handled in service layer
    /// </summary>
    public interface ISiteRepository
    {
        /// <summary>
        /// Get all basic site information
        /// Pure data access - returns raw site data from configuration
        /// </summary>
        /// <returns>List of all site basic information</returns>
        Task<IEnumerable<SiteInfo>> GetAllSiteInfoAsync();

        /// <summary>
        /// Get basic site information by ID
        /// Pure data access - no status calculations
        /// </summary>
        /// <param name="id">Site ID</param>
        /// <returns>Basic site info or null if not found</returns>
        Task<SiteInfo?> GetSiteInfoAsync(int id);

        /// <summary>
        /// Check if a site exists
        /// Pure data access - simple existence check
        /// </summary>
        /// <param name="id">Site ID to check</param>
        /// <returns>True if site exists</returns>
        Task<bool> SiteExistsAsync(int id);

        /// <summary>
        /// Get sites for a specific county
        /// Pure data access - simple filtering
        /// </summary>
        /// <param name="county">County name</param>
        /// <returns>List of sites in the county</returns>
        Task<IEnumerable<SiteInfo>> GetSitesByCountyNameAsync(string county);

        /// <summary>
        /// Get all distinct county names
        /// Pure data access - returns unique county list
        /// </summary>
        /// <returns>List of county names</returns>
        Task<IEnumerable<string>> GetAllCountiesAsync();
    }
}