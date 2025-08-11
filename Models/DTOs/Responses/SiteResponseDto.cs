// File: Models/DTOs/Responses/SiteResponseDto.cs
// Response DTO for site information

using System;
using System.Collections.Generic;

namespace GasFireMonitoringServer.Models.DTOs.Responses
{
    /// <summary>
    /// Response DTO for site information
    /// </summary>
    public class SiteResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string County { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = ""; // "normal", "alarm", "error", "offline"
        public int TotalSensors { get; set; }
        public int NormalSensors { get; set; }
        public int AlarmSensors { get; set; }
        public int ErrorSensors { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool HasCustomLayout { get; set; }
        public string LayoutMode { get; set; } = ""; // "grid" or "svg"

        // Additional computed properties for client display
        public string StatusDisplayText => Status switch
        {
            "normal" => "Normal Operation",
            "alarm" => "Alarm Condition",
            "error" => "System Error",
            "offline" => "Offline",
            _ => "Unknown"
        };

        public double AlarmPercentage => TotalSensors > 0 ? (double)AlarmSensors / TotalSensors * 100 : 0;
        public bool IsOnline => Status != "offline";
        public string LastUpdateText => LastUpdate.ToString("yyyy-MM-dd HH:mm:ss");
    }
}