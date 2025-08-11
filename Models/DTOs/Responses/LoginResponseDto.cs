// File: Models/DTOs/Responses/LoginResponseDto.cs
// Response DTO for successful login authentication

namespace GasFireMonitoringServer.Models.DTOs.Responses
{
    /// <summary>
    /// Data transfer object for login response
    /// Contains JWT token and user information after successful authentication
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// JWT bearer token for API authentication
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token type (always "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration date and time (UTC)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// User information after successful login
        /// </summary>
        public UserInfoDto User { get; set; } = new();
    }

    /// <summary>
    /// User information included in login response
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>
        /// Username of authenticated user
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User role (CEO, Regional, Operator)
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the user
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Counties this user can access
        /// </summary>
        public List<string> AllowedCounties { get; set; } = new();

        /// <summary>
        /// Site IDs this user can access
        /// </summary>
        public List<int> AllowedSites { get; set; } = new();

        /// <summary>
        /// Permissions for this user
        /// </summary>
        public List<string> Permissions { get; set; } = new();
    }
}