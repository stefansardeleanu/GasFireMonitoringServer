// File: Controllers/AuthController.cs
// Handles user authentication (login)

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace GasFireMonitoringServer.Controllers
{
    // [ApiController] tells ASP.NET this is a REST API controller
    // [Route] defines the URL pattern: /api/auth/...
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // For now, simple hardcoded authentication
            // In production, check against database with hashed passwords

            var users = new Dictionary<string, UserInfo>
            {
                ["admin"] = new UserInfo
                {
                    Password = "admin123",
                    Role = "CEO",
                    AllowedSites = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                },
                ["operator1"] = new UserInfo
                {
                    Password = "op123",
                    Role = "Operator",
                    AllowedSites = new List<int> { 5 }  // Only PanouHurezani
                },
                ["regional1"] = new UserInfo
                {
                    Password = "reg123",
                    Role = "Regional",
                    AllowedSites = new List<int> { 5, 6 }
                }
            };

            // Check credentials
            if (users.ContainsKey(request.Username) &&
                users[request.Username].Password == request.Password)
            {
                var userInfo = users[request.Username];

                // Create response
                var response = new
                {
                    success = true,
                    data = new
                    {
                        username = request.Username,
                        role = userInfo.Role,
                        token = $"fake-jwt-token-{request.Username}",  // In production, generate real JWT
                        allowedCounties = userInfo.Role == "CEO" ?
                            new List<string> { "All" } :
                            new List<string> { "Gorj" },
                        allowedSites = userInfo.AllowedSites
                    }
                };

                return Ok(response);
            }

            // Invalid credentials
            return Ok(new { success = false, message = "Invalid credentials" });
        }

        // Helper classes
        public class LoginRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        private class UserInfo
        {
            public string Password { get; set; } = "";
            public string Role { get; set; } = "";
            public List<int> AllowedSites { get; set; } = new();
        }
    }
}