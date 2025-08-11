// File: Models/DTOs/Requests/LoginRequestDto.cs
// Request DTO for user login authentication

using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Models.DTOs.Requests
{
    /// <summary>
    /// Data transfer object for login requests
    /// Contains user credentials for authentication
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Username for authentication
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password for authentication
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Remember login for extended session
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}