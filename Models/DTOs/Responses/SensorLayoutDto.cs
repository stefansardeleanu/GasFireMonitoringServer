// File: Models/DTOs/Responses/SensorLayoutDto.cs
// Response DTO for sensor positioning data on layouts

using System;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Models.DTOs.Responses
{
    /// <summary>
    /// Response DTO for sensor positioning data on SVG layouts or grid layouts
    /// </summary>
    public class SensorLayoutDto
    {
        public int SiteId { get; set; }
        public string ChannelId { get; set; } = "";
        public string TagName { get; set; } = "";
        public string DisplayName { get; set; } = "";

        // Layout positioning (percentage coordinates 0-100)
        public double LayoutX { get; set; }
        public double LayoutY { get; set; }

        // Current sensor data for real-time display
        public double ProcessValue { get; set; }
        public double CurrentValue { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = "";
        public string Units { get; set; } = "";
        public int DetectorType { get; set; }
        public string DetectorTypeName { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        // Computed properties for layout rendering
        public bool IsOnline => (DateTime.UtcNow - LastUpdated).TotalMinutes < 5;
        public bool HasAlarm => Status > 0 && Status <= 2;
        public bool HasError => Status > 2;
        public bool IsValidPosition => LayoutX >= 0 && LayoutX <= 100 && LayoutY >= 0 && LayoutY <= 100;

        // CSS classes for client styling
        public string StatusCssClass => Status switch
        {
            0 => "sensor-normal",
            1 or 2 => "sensor-alarm",
            _ => "sensor-error"
        };

        public string OnlineStatusCssClass => IsOnline ? "sensor-online" : "sensor-offline";

        // Display formatting
        public string FormattedValue => $"{ProcessValue:F2} {Units}";
        public string TooltipText => $"{DisplayName}\nValue: {FormattedValue}\nStatus: {StatusText}\nLast Update: {LastUpdated:yyyy-MM-dd HH:mm:ss}";
    }
}