// File: Controllers/TestAuthController.cs
// Test controller to verify JWT authentication is working
// DELETE this file after testing authentication

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// Test controller to verify JWT authentication
    /// Remove this controller after authentication testing is complete
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestAuthController : ControllerBase
    {
        private readonly ILogger<TestAuthController> _logger;

        public TestAuthController(ILogger<TestAuthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpGet("public")]
        public IActionResult GetPublic()
        {
            return Ok(new
            {
                message = "This is a public endpoint - no authentication required",
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

        /// <summary>
        /// Protected endpoint - JWT token required
        /// </summary>
        [HttpGet("protected")]
        [Authorize]
        public IActionResult GetProtected()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new
            {
                message = "This is a protected endpoint - JWT token required",
                user = new
                {
                    id = userId,
                    username = username,
                    role = role,
                    claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                },
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

        /// <summary>
        /// CEO only endpoint - role-based authorization
        /// </summary>
        [HttpGet("ceo-only")]
        [Authorize(Policy = "CEOOnly")]
        public IActionResult GetCeoOnly()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                message = "This endpoint is for CEO role only",
                username = username,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

        /// <summary>
        /// Regional or above endpoint - multiple roles allowed
        /// </summary>
        [HttpGet("regional-or-above")]
        [Authorize(Policy = "RegionalOrAbove")]
        public IActionResult GetRegionalOrAbove()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                message = "This endpoint is for Regional and CEO roles",
                username = username,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

        /// <summary>
        /// Permission-based endpoint - requires specific permission
        /// </summary>
        [HttpGet("manage-configuration")]
        [Authorize(Policy = "CanManageConfiguration")]
        public IActionResult GetManageConfiguration()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

            return Ok(new
            {
                message = "This endpoint requires ManageConfiguration permission",
                username = username,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                permissions = permissions,
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

        /// <summary>
        /// Get current user information from JWT token
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var allowedSites = User.FindAll("allowed_site").Select(c => int.Parse(c.Value)).ToList();
            var allowedCounties = User.FindAll("allowed_county").Select(c => c.Value).ToList();
            var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

            return Ok(new
            {
                message = "Current user information from JWT token",
                user = new
                {
                    id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    username = User.FindFirst(ClaimTypes.Name)?.Value,
                    role = User.FindFirst(ClaimTypes.Role)?.Value,
                    displayName = User.FindFirst("display_name")?.Value,
                    isActive = User.FindFirst("is_active")?.Value,
                    allowedSites = allowedSites,
                    allowedCounties = allowedCounties,
                    permissions = permissions
                },
                token = new
                {
                    jti = User.FindFirst("jti")?.Value,
                    iat = User.FindFirst("iat")?.Value
                },
                timestamp = DateTime.UtcNow,
                success = true
            });
        }
    }
}