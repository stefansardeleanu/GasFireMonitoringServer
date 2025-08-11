// File: Services/Business/Interfaces/IAuthenticationService.cs
// Authentication service interface for JWT token generation and user validation

using GasFireMonitoringServer.Models.DTOs.Requests;
using GasFireMonitoringServer.Models.DTOs.Responses;
using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Services.Business.Interfaces
{
    /// <summary>
    /// Authentication service interface for JWT token generation and user management
    /// Handles login, token generation, password validation, and user authorization
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        /// <param name="loginRequest">Login credentials</param>
        /// <returns>Login response with JWT token if successful</returns>
        Task<(bool Success, LoginResponseDto? Response, string ErrorMessage)> LoginAsync(LoginRequestDto loginRequest);

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token string</returns>
        string GenerateJwtToken(User user);

        /// <summary>
        /// Validate JWT token and get user information
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>User information if token is valid</returns>
        Task<User?> ValidateTokenAsync(string token);

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <returns>User entity if found</returns>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Verify password against stored hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="passwordHash">Stored BCrypt hash</param>
        /// <returns>True if password matches</returns>
        bool VerifyPassword(string password, string passwordHash);

        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>BCrypt hash</returns>
        string HashPassword(string password);

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateLastLoginAsync(int userId);

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        /// <param name="username">Username that failed login</param>
        /// <returns>Success indicator</returns>
        Task<bool> RecordFailedLoginAsync(string username);

        /// <summary>
        /// Check if user account is locked
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if account is locked</returns>
        Task<bool> IsAccountLockedAsync(string username);

        /// <summary>
        /// Unlock user account (admin function)
        /// </summary>
        /// <param name="username">Username to unlock</param>
        /// <returns>Success indicator</returns>
        Task<bool> UnlockAccountAsync(string username);

        /// <summary>
        /// Change user password (requires old password verification)
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="oldPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Success indicator and error message</returns>
        Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string username, string oldPassword, string newPassword);

        /// <summary>
        /// Check if user has permission to access a site
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="siteId">Site ID to check access for</param>
        /// <returns>True if user can access the site</returns>
        bool CanUserAccessSite(User user, int siteId);

        /// <summary>
        /// Check if user has permission to access a county
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="county">County name to check access for</param>
        /// <returns>True if user can access the county</returns>
        bool CanUserAccessCounty(User user, string county);

        /// <summary>
        /// Check if user has a specific permission
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="permission">Permission to check for</param>
        /// <returns>True if user has the permission</returns>
        bool UserHasPermission(User user, string permission);
    }
}