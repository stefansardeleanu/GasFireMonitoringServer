// File: Models/DTOs/SensorDto.cs
namespace GasFireMonitoringServer.Models.DTOs
{
    public class SensorDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string ChannelId { get; set; } = "";
        public string TagName { get; set; } = "";
        public string DetectorTypeName { get; set; } = "";
        public double ProcessValue { get; set; }
        public double CurrentValue { get; set; }
        public string StatusText { get; set; } = "";
        public string Units { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public bool IsOnline { get; set; } // True if updated within last 5 minutes
        public string DisplayName => $"{TagName} (CH{ChannelId})";
    }
}