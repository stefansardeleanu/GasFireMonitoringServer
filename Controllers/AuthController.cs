// File: Controllers/AuthController.cs
// Handles user authentication (login) - REFACTORED to use JWT service
// UPDATED: Now uses IAuthenticationService and proper JWT tokens

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Requests;
using GasFireMonitoringServer.Models.DTOs.Responses;
using GasFireMonitoringServer.Models.DTOs.Common;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// Authentication API controller
    /// Handles user login and JWT token generation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authenticationService,
            ILogger<AuthController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information if successful</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
        public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", request.Username);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login failed - missing username or password");
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "Username and password are required"));
                }

                // Attempt authentication
                var (success, response, errorMessage) = await _authenticationService.LoginAsync(request);

                if (success && response != null)
                {
                    _logger.LogInformation("Login successful for username: {Username} with role: {Role}",
                        request.Username, response.User.Role);

                    return Ok(ApiResponseDto<LoginResponseDto>.SuccessResult(
                        response,
                        1,
                        "Login successful"));
                }
                else
                {
                    _logger.LogWarning("Login failed for username: {Username} - {ErrorMessage}",
                        request.Username, errorMessage);

                    return Unauthorized(ApiResponseDto<object>.ErrorResult(errorMessage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An internal error occurred during login"));
            }
        }

        /// <summary>
        /// Validate current JWT token and get user information
        /// </summary>
        /// <returns>Current user information from JWT token</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<UserInfoDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponseDto<UserInfoDto>>> GetCurrentUser()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("GetCurrentUser failed - no username in token");
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("Invalid token"));
                }

                _logger.LogDebug("Getting current user info for: {Username}", username);

                var user = await _authenticationService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("GetCurrentUser failed - user not found: {Username}", username);
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("User not found"));
                }

                var userInfo = new UserInfoDto
                {
                    Username = user.Username,
                    Role = user.Role,
                    DisplayName = user.DisplayName,
                    AllowedCounties = user.AllowedCounties,
                    AllowedSites = user.AllowedSites,
                    Permissions = user.Permissions
                };

                return Ok(ApiResponseDto<UserInfoDto>.SuccessResult(
                    userInfo,
                    1,
                    "User information retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user information");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while retrieving user information"));
            }
        }

        /// <summary>
        /// Change user password (requires current password)
        /// </summary>
        /// <param name="request">Password change request</param>
        /// <returns>Success or error response</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ApiResponseDto<object>>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("Invalid token"));
                }

                _logger.LogInformation("Password change request for user: {Username}", username);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "Current password and new password are required"));
                }

                if (request.NewPassword.Length < 6)
                {
                    return BadRequest(ApiResponseDto<object>.ErrorResult(
                        "New password must be at least 6 characters long"));
                }

                // Attempt password change
                var (success, errorMessage) = await _authenticationService.ChangePasswordAsync(
                    username, request.CurrentPassword, request.NewPassword);

                if (success)
                {
                    _logger.LogInformation("Password changed successfully for user: {Username}", username);
                    return Ok(ApiResponseDto<object>.SuccessResult(
                        null,
                        0,
                        "Password changed successfully"));
                }
                else
                {
                    _logger.LogWarning("Password change failed for user: {Username} - {ErrorMessage}",
                        username, errorMessage);
                    return BadRequest(ApiResponseDto<object>.ErrorResult(errorMessage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", User.Identity?.Name);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred while changing password"));
            }
        }

        /// <summary>
        /// Logout user (client-side token removal)
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        public IActionResult Logout()
        {
            try
            {
                var username = User.Identity?.Name;
                _logger.LogInformation("User logout: {Username}", username);

                // JWT tokens are stateless, so logout is primarily client-side
                // The client should remove the token from storage
                return Ok(ApiResponseDto<object>.SuccessResult(
                    null,
                    0,
                    "Logout successful. Please remove the token from client storage."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult(
                    "An error occurred during logout"));
            }
        }
    }

    /// <summary>
    /// Password change request DTO
    /// </summary>
    public class ChangePasswordRequestDto
    {
        /// <summary>
        /// Current password for verification
        /// </summary>
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password to set
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}