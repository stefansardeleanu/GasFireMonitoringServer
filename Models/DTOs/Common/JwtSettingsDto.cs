// File: Models/DTOs/Common/JwtSettingsDto.cs
// Configuration DTO for JWT token settings

namespace GasFireMonitoringServer.Models.DTOs.Common
{
    /// <summary>
    /// Configuration settings for JWT token generation and validation
    /// Maps to JwtSettings section in appsettings.json
    /// </summary>
    public class JwtSettingsDto
    {
        /// <summary>
        /// Secret key for signing JWT tokens
        /// Should be at least 256 bits (32 characters) for HS256
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Token issuer (who created the token)
        /// Usually your application name
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token audience (who the token is intended for)
        /// Usually your client application name
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time in days
        /// Default: 7 days
        /// </summary>
        public int ExpirationDays { get; set; } = 7;

        /// <summary>
        /// Token expiration time in hours (alternative to days)
        /// If set, takes precedence over ExpirationDays
        /// </summary>
        public int? ExpirationHours { get; set; }

        /// <summary>
        /// Token expiration time in minutes (for testing/development)
        /// If set, takes precedence over ExpirationDays and ExpirationHours
        /// </summary>
        public int? ExpirationMinutes { get; set; }

        /// <summary>
        /// Whether to validate token issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate token audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Whether to validate token signature
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Clock skew allowance for token validation
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Calculate token expiration based on configuration
        /// </summary>
        /// <returns>DateTime when token should expire</returns>
        public DateTime GetExpirationTime()
        {
            var now = DateTime.UtcNow;

            if (ExpirationMinutes.HasValue)
                return now.AddMinutes(ExpirationMinutes.Value);

            if (ExpirationHours.HasValue)
                return now.AddHours(ExpirationHours.Value);

            return now.AddDays(ExpirationDays);
        }

        /// <summary>
        /// Get expiration timespan for token generation
        /// </summary>
        /// <returns>TimeSpan for token expiration</returns>
        public TimeSpan GetExpirationTimeSpan()
        {
            if (ExpirationMinutes.HasValue)
                return TimeSpan.FromMinutes(ExpirationMinutes.Value);

            if (ExpirationHours.HasValue)
                return TimeSpan.FromHours(ExpirationHours.Value);

            return TimeSpan.FromDays(ExpirationDays);
        }

        /// <summary>
        /// Validate JWT settings configuration
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Secret))
                errors.Add("JWT Secret is required");
            else if (Secret.Length < 32)
                errors.Add("JWT Secret should be at least 32 characters for security");

            if (string.IsNullOrWhiteSpace(Issuer))
                errors.Add("JWT Issuer is required");

            if (string.IsNullOrWhiteSpace(Audience))
                errors.Add("JWT Audience is required");

            if (ExpirationDays <= 0 && !ExpirationHours.HasValue && !ExpirationMinutes.HasValue)
                errors.Add("JWT expiration time must be positive");

            return errors;
        }
    }
}