// File: Models/DTOs/LoginResponse.cs
namespace GasFireMonitoringServer.Models.DTOs
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public UserData? Data { get; set; }
    }

    public class UserData
    {
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";
        public List<string> AllowedCounties { get; set; } = new();
        public List<int> AllowedSites { get; set; } = new();
    }
}