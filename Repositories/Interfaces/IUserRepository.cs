// File: Repositories/Interfaces/IUserRepository.cs
// Repository interface for user data access operations

using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for user data operations
    /// Abstracts all database operations for users
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username to search for</param>
        /// <returns>User entity if found</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User entity if found</returns>
        Task<User?> GetByIdAsync(int id);

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of all users</returns>
        Task<IEnumerable<User>> GetAllAsync();

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="user">User entity to create</param>
        /// <returns>Created user with ID assigned</returns>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// Update existing user
        /// </summary>
        /// <param name="user">User entity with updated values</param>
        /// <returns>Updated user entity</returns>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Delete user by ID
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Check if username exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if username exists</returns>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="loginTime">Login timestamp (UTC)</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateLastLoginAsync(int userId, DateTime loginTime);

        /// <summary>
        /// Increment failed login attempts
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Current number of failed attempts</returns>
        Task<int> IncrementFailedLoginAttemptsAsync(string username);

        /// <summary>
        /// Reset failed login attempts to zero
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>True if reset successfully</returns>
        Task<bool> ResetFailedLoginAttemptsAsync(string username);

        /// <summary>
        /// Lock user account until specified time
        /// </summary>
        /// <param name="username">Username to lock</param>
        /// <param name="lockedUntil">When to unlock the account (UTC)</param>
        /// <returns>True if locked successfully</returns>
        Task<bool> LockAccountAsync(string username, DateTime lockedUntil);

        /// <summary>
        /// Unlock user account
        /// </summary>
        /// <param name="username">Username to unlock</param>
        /// <returns>True if unlocked successfully</returns>
        Task<bool> UnlockAccountAsync(string username);

        /// <summary>
        /// Update user password hash
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="passwordHash">New BCrypt password hash</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash);

        /// <summary>
        /// Get active users (is_active = true)
        /// </summary>
        /// <returns>List of active users</returns>
        Task<IEnumerable<User>> GetActiveUsersAsync();

        /// <summary>
        /// Get users by role
        /// </summary>
        /// <param name="role">Role to filter by</param>
        /// <returns>List of users with specified role</returns>
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    }
}