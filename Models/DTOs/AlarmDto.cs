// File: Models/DTOs/AlarmDto.cs
namespace GasFireMonitoringServer.Models.DTOs
{
    public class AlarmDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string SensorTag { get; set; } = "";
        public string ChannelId { get; set; } = "";
        public string AlarmTypeName { get; set; } = "";
        public int AlarmLevel { get; set; }
        public double Value { get; set; }
        public string Units { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string RawMessage { get; set; } = "";
        public int MinutesAgo => (int)(DateTime.UtcNow - Timestamp).TotalMinutes;
        public string AlarmLevelText => $"Level {AlarmLevel}";
        public string DisplayText => $"{SensorTag} - {AlarmTypeName} (Level {AlarmLevel})";
    }
}