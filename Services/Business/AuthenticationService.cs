// File: Services/Business/AuthenticationService.cs
// Authentication service implementation for JWT token generation and user validation

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.DTOs.Requests;
using GasFireMonitoringServer.Models.DTOs.Responses;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Authentication service implementation
    /// Handles JWT token generation, user validation, and authorization
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtSettingsDto _jwtSettings;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public AuthenticationService(
            IUserRepository userRepository,
            IOptions<JwtSettingsDto> jwtSettings,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        public async Task<(bool Success, LoginResponseDto? Response, string ErrorMessage)> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", loginRequest.Username);

                // Get user from database
                var user = await _userRepository.GetByUsernameAsync(loginRequest.Username);
                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Username}", loginRequest.Username);
                    await RecordFailedLoginAsync(loginRequest.Username);
                    return (false, null, "Invalid username or password");
                }

                // Check if account is locked
                if (user.IsLocked)
                {
                    _logger.LogWarning("Login failed - account locked: {Username} until {LockedUntil}",
                        loginRequest.Username, user.LockedUntil);
                    return (false, null, $"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm} UTC");
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed - account disabled: {Username}", loginRequest.Username);
                    return (false, null, "Account is disabled");
                }

                // Verify password
                if (!VerifyPassword(loginRequest.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed - invalid password: {Username}", loginRequest.Username);
                    await RecordFailedLoginAsync(loginRequest.Username);
                    return (false, null, "Invalid username or password");
                }

                // Successful login - reset failed attempts and update last login
                await _userRepository.ResetFailedLoginAttemptsAsync(user.Username);
                await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

                // Generate JWT token
                var token = GenerateJwtToken(user);

                // Create response
                var response = new LoginResponseDto
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = _jwtSettings.GetExpirationTime(),
                    User = new UserInfoDto
                    {
                        Username = user.Username,
                        Role = user.Role,
                        DisplayName = user.DisplayName,
                        AllowedCounties = user.AllowedCounties,
                        AllowedSites = user.AllowedSites,
                        Permissions = user.Permissions
                    }
                };

                _logger.LogInformation("Login successful for user: {Username} with role: {Role}",
                    user.Username, user.Role);

                return (true, response, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", loginRequest.Username);
                return (false, null, "An error occurred during login");
            }
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            try
            {
                _logger.LogDebug("Generating JWT token for user: {Username}", user.Username);

                var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
                var signingKey = new SymmetricSecurityKey(key);
                var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("display_name", user.DisplayName),
                    new Claim("user_id", user.Id.ToString()),
                    new Claim("is_active", user.IsActive.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                };

                // Add allowed sites as claims
                foreach (var siteId in user.AllowedSites)
                {
                    claims.Add(new Claim("allowed_site", siteId.ToString()));
                }

                // Add allowed counties as claims
                foreach (var county in user.AllowedCounties)
                {
                    claims.Add(new Claim("allowed_county", county));
                }

                // Add permissions as claims
                foreach (var permission in user.Permissions)
                {
                    claims.Add(new Claim("permission", permission));
                }

                // Create token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = _jwtSettings.GetExpirationTime(),
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = credentials
                };

                // Generate token
                var token = _tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = _tokenHandler.WriteToken(token);

                _logger.LogDebug("JWT token generated successfully for user: {Username}", user.Username);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {Username}", user.Username);
                throw;
            }
        }

        /// <summary>
        /// Validate JWT token and get user information
        /// </summary>
        public async Task<User?> ValidateTokenAsync(string token)
        {
            try
            {
                _logger.LogDebug("Validating JWT token");

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogDebug("Token validation failed - empty token");
                    return null;
                }

                var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = _jwtSettings.ValidateIssuer,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = _jwtSettings.ValidateAudience,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = _jwtSettings.ValidateLifetime,
                    ClockSkew = _jwtSettings.ClockSkew
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Token validation failed - invalid algorithm");
                    return null;
                }

                // Extract user ID from claims
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Token validation failed - invalid user ID claim");
                    return null;
                }

                // Get user from database
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Token validation failed - user not found or inactive: {UserId}", userId);
                    return null;
                }

                _logger.LogDebug("Token validated successfully for user: {Username}", user.Username);
                return user;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Token validation failed - token expired");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed - security token exception: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
                return null;
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                _logger.LogDebug("Getting user by username: {Username}", username);
                return await _userRepository.GetByUsernameAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Verify password against stored hash
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
                {
                    _logger.LogDebug("Password verification failed - empty password or hash");
                    return false;
                }

                var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                _logger.LogDebug("Password verification result: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        public string HashPassword(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                var hash = BCrypt.Net.BCrypt.HashPassword(password, 12); // Work factor 12 for security
                _logger.LogDebug("Password hashed successfully");
                return hash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw;
            }
        }

        /// <summary>
        /// Update user's last login timestamp
        /// </summary>
        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                _logger.LogDebug("Updating last login for user ID: {UserId}", userId);
                return await _userRepository.UpdateLastLoginAsync(userId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        public async Task<bool> RecordFailedLoginAsync(string username)
        {
            try
            {
                _logger.LogDebug("Recording failed login attempt for: {Username}", username);
                var attempts = await _userRepository.IncrementFailedLoginAttemptsAsync(username);

                if (attempts >= 5)
                {
                    _logger.LogWarning("Account locked due to failed login attempts: {Username}", username);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording failed login attempt for: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Check if user account is locked
        /// </summary>
        public async Task<bool> IsAccountLockedAsync(string username)
        {
            try
            {
                _logger.LogDebug("Checking if account is locked: {Username}", username);
                var user = await _userRepository.GetByUsernameAsync(username);
                return user?.IsLocked ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if account is locked: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Unlock user account (admin function)
        /// </summary>
        public async Task<bool> UnlockAccountAsync(string username)
        {
            try
            {
                _logger.LogInformation("Unlocking account: {Username}", username);
                return await _userRepository.UnlockAccountAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Change user password (requires old password verification)
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            try
            {
                _logger.LogInformation("Password change request for user: {Username}", username);

                // Get user
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Password change failed - user not found: {Username}", username);
                    return (false, "User not found");
                }

                // Verify old password
                if (!VerifyPassword(oldPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed - invalid old password: {Username}", username);
                    return (false, "Invalid current password");
                }

                // Validate new password
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    return (false, "New password must be at least 6 characters long");
                }

                // Hash new password
                var newPasswordHash = HashPassword(newPassword);

                // Update password
                var success = await _userRepository.UpdatePasswordAsync(user.Id, newPasswordHash);
                if (success)
                {
                    _logger.LogInformation("Password changed successfully for user: {Username}", username);
                    return (true, string.Empty);
                }
                else
                {
                    _logger.LogError("Failed to update password in database for user: {Username}", username);
                    return (false, "Failed to update password");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", username);
                return (false, "An error occurred while changing password");
            }
        }

        /// <summary>
        /// Check if user has permission to access a site
        /// </summary>
        public bool CanUserAccessSite(User user, int siteId)
        {
            try
            {
                var canAccess = user.CanAccessSite(siteId);
                _logger.LogDebug("Site access check for user {Username} to site {SiteId}: {CanAccess}",
                    user.Username, siteId, canAccess);
                return canAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking site access for user: {Username}, site: {SiteId}",
                    user.Username, siteId);
                return false;
            }
        }

        /// <summary>
        /// Check if user has permission to access a county
        /// </summary>
        public bool CanUserAccessCounty(User user, string county)
        {
            try
            {
                var canAccess = user.CanAccessCounty(county);
                _logger.LogDebug("County access check for user {Username} to county {County}: {CanAccess}",
                    user.Username, county, canAccess);
                return canAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking county access for user: {Username}, county: {County}",
                    user.Username, county);
                return false;
            }
        }

        /// <summary>
        /// Check if user has a specific permission
        /// </summary>
        public bool UserHasPermission(User user, string permission)
        {
            try
            {
                var hasPermission = user.HasPermission(permission);
                _logger.LogDebug("Permission check for user {Username} for permission {Permission}: {HasPermission}",
                    user.Username, permission, hasPermission);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for user: {Username}, permission: {Permission}",
                    user.Username, permission);
                return false;
            }
        }
    }
}