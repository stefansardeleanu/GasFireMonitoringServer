// File: Repositories/UserRepository.cs
// Repository implementation for user data access operations

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;

namespace GasFireMonitoringServer.Repositories
{
    /// <summary>
    /// Repository implementation for user data operations
    /// Handles all database interactions for users
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                _logger.LogDebug("Getting user by username: {Username}", username);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                if (user != null)
                {
                    _logger.LogDebug("Found user: {Username} with role: {Role}", user.Username, user.Role);
                }
                else
                {
                    _logger.LogDebug("User not found: {Username}", username);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting user by ID: {UserId}", id);

                var user = await _context.Users.FindAsync(id);

                if (user != null)
                {
                    _logger.LogDebug("Found user: {Username} (ID: {UserId})", user.Username, user.Id);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Getting all users");

                var users = await _context.Users
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                _logger.LogDebug("Creating new user: {Username}", user.Username);

                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created user: {Username} (ID: {UserId})", user.Username, user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                throw;
            }
        }

        /// <summary>
        /// Update existing user
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            try
            {
                _logger.LogDebug("Updating user: {Username} (ID: {UserId})", user.Username, user.Id);

                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user: {Username} (ID: {UserId})", user.Username, user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {Username} (ID: {UserId})", user.Username, user.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete user by ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogDebug("Deleting user by ID: {UserId}", id);

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for deletion: {UserId}", id);
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted user: {Username} (ID: {UserId})", user.Username, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Check if username exists
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                _logger.LogDebug("Checking if username exists: {Username}", username);

                var exists = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == username.ToLower());

                _logger.LogDebug("Username {Username} exists: {Exists}", username, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username exists: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        public async Task<bool> UpdateLastLoginAsync(int userId, DateTime loginTime)
        {
            try
            {
                _logger.LogDebug("Updating last login for user ID: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for last login update: {UserId}", userId);
                    return false;
                }

                user.LastLoginAt = loginTime;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated last login for user: {Username} (ID: {UserId})", user.Username, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user ID: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Increment failed login attempts
        /// </summary>
        public async Task<int> IncrementFailedLoginAttemptsAsync(string username)
        {
            try
            {
                _logger.LogDebug("Incrementing failed login attempts for: {Username}", username);

                var user = await GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User not found for failed login increment: {Username}", username);
                    return 0;
                }

                user.FailedLoginAttempts++;
                user.UpdatedAt = DateTime.UtcNow;

                // Lock account after 5 failed attempts for 30 minutes
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("Account locked due to failed attempts: {Username}", username);
                }

                await _context.SaveChangesAsync();

                _logger.LogDebug("Failed login attempts for {Username}: {Attempts}", username, user.FailedLoginAttempts);
                return user.FailedLoginAttempts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failed login attempts for: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Reset failed login attempts to zero
        /// </summary>
        public async Task<bool> ResetFailedLoginAttemptsAsync(string username)
        {
            try
            {
                _logger.LogDebug("Resetting failed login attempts for: {Username}", username);

                var user = await GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User not found for failed login reset: {Username}", username);
                    return false;
                }

                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Reset failed login attempts for: {Username}", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting failed login attempts for: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Lock user account until specified time
        /// </summary>
        public async Task<bool> LockAccountAsync(string username, DateTime lockedUntil)
        {
            try
            {
                _logger.LogDebug("Locking account: {Username} until {LockedUntil}", username, lockedUntil);

                var user = await GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User not found for account lock: {Username}", username);
                    return false;
                }

                user.LockedUntil = lockedUntil;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Locked account: {Username} until {LockedUntil}", username, lockedUntil);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking account: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Unlock user account
        /// </summary>
        public async Task<bool> UnlockAccountAsync(string username)
        {
            try
            {
                _logger.LogDebug("Unlocking account: {Username}", username);

                var user = await GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User not found for account unlock: {Username}", username);
                    return false;
                }

                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Unlocked account: {Username}", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Update user password hash
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            try
            {
                _logger.LogDebug("Updating password for user ID: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for password update: {UserId}", userId);
                    return false;
                }

                user.PasswordHash = passwordHash;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated password for user: {Username} (ID: {UserId})", user.Username, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user ID: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get active users (is_active = true)
        /// </summary>
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            try
            {
                _logger.LogDebug("Getting active users");

                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} active users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                throw;
            }
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            try
            {
                _logger.LogDebug("Getting users by role: {Role}", role);

                var users = await _context.Users
                    .Where(u => u.Role.ToLower() == role.ToLower())
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} users with role: {Role}", users.Count, role);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                throw;
            }
        }
    }
}