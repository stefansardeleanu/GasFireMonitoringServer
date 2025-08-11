// File: Models/Entities/User.cs
// User entity for authentication and authorization

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFireMonitoringServer.Models.Entities
{
    /// <summary>
    /// User entity for authentication and authorization
    /// Stores user credentials and access permissions
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Primary key - User ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique username for login
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User role (CEO, Regional, Operator)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the user
        /// </summary>
        [StringLength(100)]
        [Column("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Email address (optional)
        /// </summary>
        [StringLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Whether user account is active
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Counties this user can access (JSON array)
        /// </summary>
        [Column("allowed_counties")]
        public string AllowedCountiesJson { get; set; } = "[]";

        /// <summary>
        /// Site IDs this user can access (JSON array)
        /// </summary>
        [Column("allowed_sites")]
        public string AllowedSitesJson { get; set; } = "[]";

        /// <summary>
        /// User permissions (JSON array)
        /// </summary>
        [Column("permissions")]
        public string PermissionsJson { get; set; } = "[]";

        /// <summary>
        /// When user was created
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When user was last updated
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Number of failed login attempts
        /// </summary>
        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// When account was locked due to failed attempts
        /// </summary>
        [Column("locked_until")]
        public DateTime? LockedUntil { get; set; }

        // Navigation properties for JSON fields (not mapped to database)

        /// <summary>
        /// Counties this user can access (deserialized from JSON)
        /// </summary>
        [NotMapped]
        public List<string> AllowedCounties
        {
            get => string.IsNullOrEmpty(AllowedCountiesJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(AllowedCountiesJson) ?? new List<string>();
            set => AllowedCountiesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// Site IDs this user can access (deserialized from JSON)
        /// </summary>
        [NotMapped]
        public List<int> AllowedSites
        {
            get => string.IsNullOrEmpty(AllowedSitesJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(AllowedSitesJson) ?? new List<int>();
            set => AllowedSitesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// User permissions (deserialized from JSON)
        /// </summary>
        [NotMapped]
        public List<string> Permissions
        {
            get => string.IsNullOrEmpty(PermissionsJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(PermissionsJson) ?? new List<string>();
            set => PermissionsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// Check if user account is locked
        /// </summary>
        [NotMapped]
        public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

        /// <summary>
        /// Check if user can access a specific site
        /// </summary>
        /// <param name="siteId">Site ID to check</param>
        /// <returns>True if user can access the site</returns>
        public bool CanAccessSite(int siteId)
        {
            // CEO role has access to all sites
            if (Role.Equals("CEO", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if site is in allowed sites list
            return AllowedSites.Contains(siteId);
        }

        /// <summary>
        /// Check if user can access a specific county
        /// </summary>
        /// <param name="county">County name to check</param>
        /// <returns>True if user can access the county</returns>
        public bool CanAccessCounty(string county)
        {
            // CEO role has access to all counties
            if (Role.Equals("CEO", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if county is in allowed counties list
            return AllowedCounties.Contains(county, StringComparer.OrdinalIgnoreCase) ||
                   AllowedCounties.Contains("All", StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user has a specific permission
        /// </summary>
        /// <param name="permission">Permission to check</param>
        /// <returns>True if user has the permission</returns>
        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }
    }
}