// File: Controllers/AuthController.cs
// Enhanced XML Documentation for Authentication Controller
// Handles user authentication, JWT token management, and authorization
// Phase 7.1: Comprehensive API Documentation Implementation

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Requests;
using GasFireMonitoringServer.Models.DTOs.Responses;
using GasFireMonitoringServer.Models.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// Professional Authentication API Controller for Gas Fire Monitoring System
    /// </summary>
    /// <remarks>
    /// <para><strong>Authentication Management System</strong></para>
    /// <para>This controller provides comprehensive authentication services for the Gas Fire Monitoring System, including:</para>
    /// 
    /// <para><strong>🔐 JWT Authentication System</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Stateless Authentication</strong>: JWT tokens for secure, scalable authentication</description></item>
    /// <item><description><strong>BCrypt Security</strong>: Industry-standard password hashing with salt</description></item>
    /// <item><description><strong>Token Expiration</strong>: Configurable token lifetime for security balance</description></item>
    /// <item><description><strong>Bearer Token Format</strong>: Standard HTTP Authorization header support</description></item>
    /// </list>
    /// 
    /// <para><strong>🏢 Role-Based Access Control (RBAC)</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>CEO Role</strong>: Full system access across all counties and sites</description></item>
    /// <item><description><strong>Regional Role</strong>: Access to specific counties and their sites</description></item>
    /// <item><description><strong>Operator Role</strong>: Limited access to assigned sites only</description></item>
    /// </list>
    /// 
    /// <para><strong>🎛️ Permission-Based Authorization</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>ManageConfiguration</strong>: Update site layouts and sensor positions</description></item>
    /// <item><description><strong>ManageUsers</strong>: Create, modify, and delete user accounts</description></item>
    /// <item><description><strong>ViewReports</strong>: Access historical data and analytics</description></item>
    /// <item><description><strong>ManageAlarms</strong>: Configure alarm thresholds and responses</description></item>
    /// </list>
    /// 
    /// <para><strong>🗺️ Geographic Access Control</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>County-Level Access</strong>: Users restricted to specific Romanian counties (Prahova, Gorj, etc.)</description></item>
    /// <item><description><strong>Site-Level Access</strong>: Fine-grained control over individual monitoring sites</description></item>
    /// <item><description><strong>Dynamic Permissions</strong>: Permissions can be updated without system restart</description></item>
    /// </list>
    /// 
    /// <para><strong>🔒 Security Features</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Password Validation</strong>: Enforced complexity requirements (minimum 6 characters)</description></item>
    /// <item><description><strong>Account Security</strong>: Account locking and login attempt tracking</description></item>
    /// <item><description><strong>Audit Logging</strong>: Comprehensive logging of all authentication events</description></item>
    /// <item><description><strong>Token Validation</strong>: Real-time JWT token verification</description></item>
    /// </list>
    /// 
    /// <para><strong>💼 Business Context</strong></para>
    /// <para>Authentication is critical for industrial gas and fire monitoring systems where:</para>
    /// <list type="bullet">
    /// <item><description><strong>Safety Compliance</strong>: Regulatory requirements for access control and audit trails</description></item>
    /// <item><description><strong>Data Integrity</strong>: Preventing unauthorized configuration changes that could affect safety</description></item>
    /// <item><description><strong>Operational Security</strong>: Protecting critical infrastructure monitoring systems</description></item>
    /// <item><description><strong>Multi-Site Management</strong>: Coordinated access across multiple industrial facilities</description></item>
    /// </list>
    /// 
    /// <para><strong>🚀 Usage Examples</strong></para>
    /// <para><em>Login Process:</em></para>
    /// <code>
    /// POST /api/auth/login
    /// {
    ///   "username": "operator.prahova",
    ///   "password": "SecurePass123",
    ///   "rememberMe": false
    /// }
    /// </code>
    /// 
    /// <para><em>Accessing Protected Resources:</em></para>
    /// <code>
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
    /// </code>
    /// 
    /// <para><strong>📊 Integration Points</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Configuration Controller</strong>: Site and sensor management permissions</description></item>
    /// <item><description><strong>Layout Controller</strong>: SVG layout access based on site permissions</description></item>
    /// <item><description><strong>Real-time SignalR</strong>: Authenticated connections for live monitoring</description></item>
    /// <item><description><strong>MQTT Integration</strong>: Secure sensor data ingestion</description></item>
    /// </list>
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthController with required dependencies
        /// </summary>
        /// <param name="authenticationService">Authentication service for JWT operations</param>
        /// <param name="logger">Logger for authentication events and debugging</param>
        public AuthController(
            IAuthenticationService authenticationService,
            ILogger<AuthController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user credentials and generate JWT access token
        /// </summary>
        /// <remarks>
        /// <para><strong>Primary Authentication Endpoint</strong></para>
        /// <para>This endpoint validates user credentials against the system database and generates a JWT token for authenticated access to all protected API endpoints.</para>
        /// 
        /// <para><strong>🔐 Authentication Process:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Credential Validation</strong>: Username and password are validated against database records</description></item>
        /// <item><description><strong>Password Verification</strong>: BCrypt hashing used to verify password against stored hash</description></item>
        /// <item><description><strong>User Authorization</strong>: User role and permissions are loaded from database</description></item>
        /// <item><description><strong>JWT Generation</strong>: Secure token generated with user claims and permissions</description></item>
        /// <item><description><strong>Response Assembly</strong>: Token and user information returned to client</description></item>
        /// </list>
        /// 
        /// <para><strong>🎯 Business Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Operator Login</strong>: Field operators accessing monitoring dashboards</description></item>
        /// <item><description><strong>Regional Manager Access</strong>: Supervisors monitoring multiple sites</description></item>
        /// <item><description><strong>CEO Dashboard</strong>: Executive overview of all monitoring operations</description></item>
        /// <item><description><strong>Mobile Applications</strong>: Remote access for on-call personnel</description></item>
        /// </list>
        /// 
        /// <para><strong>🔒 Security Considerations:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Rate Limiting</strong>: Login attempts should be monitored for brute force protection</description></item>
        /// <item><description><strong>Account Locking</strong>: Multiple failed attempts trigger account security measures</description></item>
        /// <item><description><strong>Audit Logging</strong>: All login attempts logged for security analysis</description></item>
        /// <item><description><strong>Token Security</strong>: JWT includes expiration and signature validation</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Performance Notes:</strong></para>
        /// <para>Login operations include database queries for user lookup and password verification. Consider caching user permissions for frequently accessed accounts.</para>
        /// 
        /// <para><strong>🛠️ Error Scenarios:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Invalid Credentials</strong>: Returns 401 with descriptive error message</description></item>
        /// <item><description><strong>Account Locked</strong>: Returns 401 with account status information</description></item>
        /// <item><description><strong>Database Error</strong>: Returns 500 with generic error message for security</description></item>
        /// <item><description><strong>Validation Failure</strong>: Returns 400 with specific field validation errors</description></item>
        /// </list>
        /// </remarks>
        /// <param name="request">User login credentials with username, password, and optional remember me flag</param>
        /// <returns>JWT access token with user information and permissions if authentication successful</returns>
        /// <response code="200">Authentication successful - Returns JWT token and user information</response>
        /// <response code="400">Invalid input - Missing or malformed login credentials</response>
        /// <response code="401">Authentication failed - Invalid username, password, or account locked</response>
        /// <response code="500">Server error - Internal authentication system error</response>
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
        /// Retrieve current authenticated user information from JWT token
        /// </summary>
        /// <remarks>
        /// <para><strong>User Information Endpoint</strong></para>
        /// <para>This endpoint validates the JWT token in the Authorization header and returns the current user's profile information, including roles, permissions, and access rights.</para>
        /// 
        /// <para><strong>🔍 Information Retrieved:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>User Profile</strong>: Username, display name, and role information</description></item>
        /// <item><description><strong>Access Rights</strong>: Counties and sites the user is authorized to access</description></item>
        /// <item><description><strong>Permissions</strong>: Specific system permissions (ManageConfiguration, ViewReports, etc.)</description></item>
        /// <item><description><strong>Role Context</strong>: CEO, Regional Manager, or Operator designation</description></item>
        /// </list>
        /// 
        /// <para><strong>💼 Business Applications:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Dashboard Personalization</strong>: Show only sites and counties user can access</description></item>
        /// <item><description><strong>Menu Configuration</strong>: Enable/disable features based on user permissions</description></item>
        /// <item><description><strong>Security Validation</strong>: Verify token validity before sensitive operations</description></item>
        /// <item><description><strong>User Experience</strong>: Display user-specific welcome messages and role indicators</description></item>
        /// </list>
        /// 
        /// <para><strong>🔐 Security Validation:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>JWT Signature</strong>: Token signature verified against server secret</description></item>
        /// <item><description><strong>Token Expiration</strong>: Expired tokens automatically rejected</description></item>
        /// <item><description><strong>User Existence</strong>: Validates user still exists in database</description></item>
        /// <item><description><strong>Account Status</strong>: Checks for account locks or deactivation</description></item>
        /// </list>
        /// 
        /// <para><strong>🚀 Client Integration:</strong></para>
        /// <para>This endpoint is commonly called on application startup to initialize user context and on periodic intervals to verify token validity.</para>
        /// 
        /// <para><strong>⚡ Performance Optimization:</strong></para>
        /// <para>User information is cached to minimize database queries. Cache invalidation occurs when user permissions change.</para>
        /// </remarks>
        /// <returns>Current user information including role, permissions, and access rights</returns>
        /// <response code="200">Success - Returns current user information</response>
        /// <response code="401">Unauthorized - Invalid, expired, or missing JWT token</response>
        /// <response code="500">Server error - Internal error retrieving user information</response>
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
        /// Change authenticated user's password with current password verification
        /// </summary>
        /// <remarks>
        /// <para><strong>Secure Password Change Endpoint</strong></para>
        /// <para>This endpoint allows authenticated users to change their password by providing their current password for verification and a new secure password.</para>
        /// 
        /// <para><strong>🔒 Security Process:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Authentication Check</strong>: Verify user is logged in with valid JWT token</description></item>
        /// <item><description><strong>Current Password Verification</strong>: Validate current password using BCrypt</description></item>
        /// <item><description><strong>New Password Validation</strong>: Ensure new password meets security requirements</description></item>
        /// <item><description><strong>Hash Generation</strong>: Create new BCrypt hash for the new password</description></item>
        /// <item><description><strong>Database Update</strong>: Store new password hash securely in database</description></item>
        /// </list>
        /// 
        /// <para><strong>💼 Business Requirements:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Compliance</strong>: Password changes may be required by security policies</description></item>
        /// <item><description><strong>Self-Service</strong>: Users can change passwords without administrator intervention</description></item>
        /// <item><description><strong>Security Incident Response</strong>: Immediate password change capability for security breaches</description></item>
        /// <item><description><strong>Regular Maintenance</strong>: Periodic password updates for enhanced security</description></item>
        /// </list>
        /// 
        /// <para><strong>🛡️ Security Features:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Current Password Required</strong>: Prevents unauthorized password changes</description></item>
        /// <item><description><strong>Password Complexity</strong>: New passwords must meet minimum security standards</description></item>
        /// <item><description><strong>Audit Logging</strong>: All password changes logged for security monitoring</description></item>
        /// <item><description><strong>BCrypt Hashing</strong>: Industry-standard password hashing with salt</description></item>
        /// </list>
        /// 
        /// <para><strong>⚠️ Important Notes:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Token Validity</strong>: Existing JWT tokens remain valid after password change</description></item>
        /// <item><description><strong>Session Management</strong>: Consider forcing re-authentication after password change</description></item>
        /// <item><description><strong>Password History</strong>: System may prevent reuse of recent passwords</description></item>
        /// </list>
        /// </remarks>
        /// <param name="request">Password change request containing current and new passwords</param>
        /// <returns>Success confirmation or error details</returns>
        /// <response code="200">Success - Password changed successfully</response>
        /// <response code="400">Validation error - Invalid current password or new password doesn't meet requirements</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="500">Server error - Internal error during password change</response>
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
                    _logger.LogWarning("ChangePassword failed - no username in token");
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("Invalid token"));
                }

                _logger.LogInformation("Password change attempt for user: {Username}", username);

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
        /// Logout current user session (client-side token removal)
        /// </summary>
        /// <remarks>
        /// <para><strong>JWT Logout Endpoint</strong></para>
        /// <para>This endpoint provides a logout mechanism for JWT-based authentication. Since JWT tokens are stateless, the primary logout action occurs on the client side by removing the token from storage.</para>
        /// 
        /// <para><strong>🔄 Logout Process:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Authentication Verification</strong>: Confirm user has valid JWT token</description></item>
        /// <item><description><strong>Logout Logging</strong>: Record logout event for audit and security monitoring</description></item>
        /// <item><description><strong>Client Instruction</strong>: Provide guidance for client-side token removal</description></item>
        /// <item><description><strong>Session Cleanup</strong>: Clear any server-side session data if applicable</description></item>
        /// </list>
        /// 
        /// <para><strong>🎯 Client Responsibilities:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Token Removal</strong>: Delete JWT token from localStorage, sessionStorage, or cookies</description></item>
        /// <item><description><strong>Memory Cleanup</strong>: Clear any cached user data or permissions</description></item>
        /// <item><description><strong>UI Updates</strong>: Redirect to login page or update authentication state</description></item>
        /// <item><description><strong>Persistent Storage</strong>: Remove any "remember me" tokens or credentials</description></item>
        /// </list>
        /// 
        /// <para><strong>💼 Business Context:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Security Compliance</strong>: Proper logout procedures required for audit compliance</description></item>
        /// <item><description><strong>Shared Workstations</strong>: Important for industrial environments with shared computers</description></item>
        /// <item><description><strong>Session Management</strong>: Clean session termination for monitoring applications</description></item>
        /// <item><description><strong>Mobile Security</strong>: Secure logout for mobile monitoring applications</description></item>
        /// </list>
        /// 
        /// <para><strong>🔒 Security Considerations:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Token Blacklisting</strong>: Consider implementing server-side token blacklist for enhanced security</description></item>
        /// <item><description><strong>Session Timeout</strong>: Complement with automatic token expiration</description></item>
        /// <item><description><strong>Concurrent Sessions</strong>: May need to handle multiple simultaneous sessions</description></item>
        /// <item><description><strong>Audit Trail</strong>: All logout events logged for security analysis</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Integration Notes:</strong></para>
        /// <para>This endpoint is typically called before page navigation or application closure to ensure proper session cleanup and security compliance.</para>
        /// </remarks>
        /// <returns>Success confirmation with logout instructions</returns>
        /// <response code="200">Success - Logout completed, client should remove token</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication token</response>
        /// <response code="500">Server error - Internal error during logout process</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
        [ProducesResponseType(401)]
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
}

#region DTOs - Authentication System Data Transfer Objects

/// <summary>
/// Password change request data transfer object
/// </summary>
/// <remarks>
/// <para><strong>Secure Password Change DTO</strong></para>
/// <para>This DTO handles password change requests with current password verification for enhanced security.</para>
/// 
/// <para><strong>🔒 Security Features:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Current Password Verification</strong>: Requires knowledge of existing password</description></item>
/// <item><description><strong>Input Validation</strong>: Ensures both passwords meet minimum requirements</description></item>
/// <item><description><strong>No Password Storage</strong>: Passwords processed immediately and not stored in memory</description></item>
/// </list>
/// 
/// <para><strong>💼 Usage Context:</strong></para>
/// <para>Used for self-service password changes by authenticated users, typically in user profile or security settings sections.</para>
/// </remarks>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// Current password for security verification
    /// </summary>
    /// <remarks>
    /// <para>The user's existing password, required to authorize the password change operation.</para>
    /// <para><strong>Security:</strong> Prevents unauthorized password changes even if session is compromised.</para>
    /// <para><strong>Validation:</strong> Must match the current password hash stored in the database.</para>
    /// </remarks>
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password to replace the current password
    /// </summary>
    /// <remarks>
    /// <para>The replacement password that will be hashed and stored securely.</para>
    /// <para><strong>Requirements:</strong> Must meet system password complexity standards (minimum 6 characters).</para>
    /// <para><strong>Security:</strong> Will be hashed using BCrypt before database storage.</para>
    /// <para><strong>Validation:</strong> Should be different from current password for security best practices.</para>
    /// </remarks>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Login request data transfer object for user authentication
/// </summary>
/// <remarks>
/// <para><strong>Authentication Credentials DTO</strong></para>
/// <para>This DTO encapsulates user login credentials with comprehensive validation for secure authentication.</para>
/// 
/// <para><strong>🔐 Security Features:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Input Validation</strong>: Username and password length requirements prevent injection attacks</description></item>
/// <item><description><strong>Required Fields</strong>: Ensures all necessary authentication data is provided</description></item>
/// <item><description><strong>Memory Safety</strong>: Credentials processed immediately without long-term storage</description></item>
/// </list>
/// 
/// <para><strong>💼 Usage Examples:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Web Dashboard</strong>: Browser-based operator login</description></item>
/// <item><description><strong>Mobile Apps</strong>: Field personnel authentication</description></item>
/// <item><description><strong>API Integration</strong>: Service-to-service authentication</description></item>
/// </list>
/// </remarks>
public class LoginRequestDto
{
    /// <summary>
    /// Username for system authentication
    /// </summary>
    /// <remarks>
    /// <para>Unique identifier for the user account within the gas monitoring system.</para>
    /// <para><strong>Format:</strong> Typically follows pattern like "operator.prahova" or "regional.manager"</para>
    /// <para><strong>Validation:</strong> Must be 3-50 characters to prevent both short and excessively long usernames</para>
    /// <para><strong>Security:</strong> Case-sensitive matching for enhanced security</para>
    /// </remarks>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for user authentication
    /// </summary>
    /// <remarks>
    /// <para>User's secret password for account verification.</para>
    /// <para><strong>Security:</strong> Minimum 6 characters required for basic security compliance</para>
    /// <para><strong>Processing:</strong> Verified against BCrypt hash stored in database</para>
    /// <para><strong>Best Practices:</strong> Should include mix of letters, numbers, and special characters</para>
    /// </remarks>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional flag for extended session duration
    /// </summary>
    /// <remarks>
    /// <para>When enabled, extends JWT token expiration for persistent sessions.</para>
    /// <para><strong>Use Case:</strong> Suitable for trusted devices and long monitoring shifts</para>
    /// <para><strong>Security:</strong> Should be disabled on shared or public computers</para>
    /// <para><strong>Default:</strong> False for security by default</para>
    /// </remarks>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Login response data transfer object containing JWT token and user information
/// </summary>
/// <remarks>
/// <para><strong>Authentication Success Response</strong></para>
/// <para>This DTO provides complete authentication response including JWT token and user context information.</para>
/// 
/// <para><strong>🎯 Response Components:</strong></para>
/// <list type="bullet">
/// <item><description><strong>JWT Token</strong>: Secure bearer token for API authentication</description></item>
/// <item><description><strong>User Context</strong>: Role, permissions, and access rights</description></item>
/// <item><description><strong>Token Metadata</strong>: Expiration time and token type</description></item>
/// </list>
/// 
/// <para><strong>💼 Client Usage:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Authorization Header</strong>: Token used in "Authorization: Bearer {token}" format</description></item>
/// <item><description><strong>User Interface</strong>: User information displayed in dashboard headers</description></item>
/// <item><description><strong>Access Control</strong>: Permissions used to enable/disable UI features</description></item>
/// </list>
/// </remarks>
public class LoginResponseDto
{
    /// <summary>
    /// JWT bearer token for API authentication
    /// </summary>
    /// <remarks>
    /// <para>Secure JSON Web Token containing user claims and permissions.</para>
    /// <para><strong>Format:</strong> Base64-encoded JWT with header.payload.signature structure</para>
    /// <para><strong>Usage:</strong> Include in Authorization header as "Bearer {token}"</para>
    /// <para><strong>Security:</strong> Signed with server secret key and includes expiration</para>
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token type identifier (always "Bearer")
    /// </summary>
    /// <remarks>
    /// <para>Standard OAuth 2.0 token type designation.</para>
    /// <para><strong>RFC Compliance:</strong> Follows RFC 6750 Bearer Token specification</para>
    /// <para><strong>Client Implementation:</strong> Used to construct proper Authorization header</para>
    /// </remarks>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration timestamp (UTC)
    /// </summary>
    /// <remarks>
    /// <para>Absolute expiration time for the JWT token in UTC timezone.</para>
    /// <para><strong>Client Usage:</strong> Monitor for token refresh or re-authentication needs</para>
    /// <para><strong>Security:</strong> Tokens automatically invalid after this time</para>
    /// <para><strong>Format:</strong> ISO 8601 DateTime format</para>
    /// </remarks>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Authenticated user information and permissions
    /// </summary>
    /// <remarks>
    /// <para>Complete user context including role, permissions, and access rights.</para>
    /// <para><strong>Purpose:</strong> Enables client-side authorization and UI customization</para>
    /// <para><strong>Contents:</strong> Username, role, display name, counties, sites, and permissions</para>
    /// </remarks>
    public UserInfoDto User { get; set; } = new();
}

/// <summary>
/// User information data transfer object for authenticated user context
/// </summary>
/// <remarks>
/// <para><strong>User Context and Authorization DTO</strong></para>
/// <para>This DTO provides comprehensive user information for client-side authorization and UI customization.</para>
/// 
/// <para><strong>🏢 Access Control Hierarchy:</strong></para>
/// <list type="bullet">
/// <item><description><strong>CEO</strong>: Full system access, all counties and sites</description></item>
/// <item><description><strong>Regional</strong>: Access to specific counties and their sites</description></item>
/// <item><description><strong>Operator</strong>: Limited access to assigned sites only</description></item>
/// </list>
/// 
/// <para><strong>🗺️ Geographic Scope:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Counties</strong>: Romanian administrative regions (Prahova, Gorj, etc.)</description></item>
/// <item><description><strong>Sites</strong>: Individual industrial monitoring locations</description></item>
/// <item><description><strong>Dynamic Access</strong>: Permissions can be updated without re-authentication</description></item>
/// </list>
/// </remarks>
public class UserInfoDto
{
    /// <summary>
    /// Unique username identifier
    /// </summary>
    /// <remarks>
    /// <para>The user's login identifier within the monitoring system.</para>
    /// <para><strong>Format:</strong> Often includes role and location context (e.g., "operator.prahova")</para>
    /// <para><strong>Uniqueness:</strong> Must be unique across the entire system</para>
    /// </remarks>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User role designation (CEO, Regional, Operator)
    /// </summary>
    /// <remarks>
    /// <para>Primary role-based access control designation.</para>
    /// <para><strong>CEO</strong>: Executive level access to all system resources</para>
    /// <para><strong>Regional</strong>: Regional manager access to assigned counties</para>
    /// <para><strong>Operator</strong>: Field operator access to specific monitoring sites</para>
    /// </remarks>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for user interface
    /// </summary>
    /// <remarks>
    /// <para>Friendly name displayed in user interface elements.</para>
    /// <para><strong>Format:</strong> Typically "First Last" or "Role Title"</para>
    /// <para><strong>Usage:</strong> Dashboard headers, welcome messages, and user menus</para>
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Romanian counties this user is authorized to access
    /// </summary>
    /// <remarks>
    /// <para>List of Romanian administrative counties where user has monitoring access.</para>
    /// <para><strong>Examples:</strong> ["Prahova", "Gorj", "Buzau"]</para>
    /// <para><strong>Usage:</strong> Filter county maps and site lists</para>
    /// <para><strong>CEO Access:</strong> Empty list typically means access to all counties</para>
    /// </remarks>
    public List<string> AllowedCounties { get; set; } = new();

    /// <summary>
    /// Monitoring site IDs this user is authorized to access
    /// </summary>
    /// <remarks>
    /// <para>Specific site identifiers where user has monitoring privileges.</para>
    /// <para><strong>Examples:</strong> [1, 2, 3, 7, 8, 9, 10] for Prahova county sites</para>
    /// <para><strong>Usage:</strong> Filter site dashboards and sensor data</para>
    /// <para><strong>CEO Access:</strong> Empty list typically means access to all sites</para>
    /// </remarks>
    public List<int> AllowedSites { get; set; } = new();

    /// <summary>
    /// Specific system permissions for fine-grained access control
    /// </summary>
    /// <remarks>
    /// <para>Granular permissions beyond role-based access control.</para>
    /// <para><strong>Available Permissions:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>ManageConfiguration</strong>: Update site layouts and sensor positions</description></item>
    /// <item><description><strong>ManageUsers</strong>: Create, modify, and delete user accounts</description></item>
    /// <item><description><strong>ViewReports</strong>: Access historical data and analytics</description></item>
    /// <item><description><strong>ManageAlarms</strong>: Configure alarm thresholds and responses</description></item>
    /// </list>
    /// <para><strong>Usage:</strong> Enable/disable specific UI features and API endpoints</para>
    /// </remarks>
    public List<string> Permissions { get; set; } = new();
}

#endregion