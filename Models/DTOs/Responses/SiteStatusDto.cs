// File: Models/DTOs/Responses/SiteStatusDto.cs
// Response DTO for dashboard summaries and site status information

using System;
using System.Collections.Generic;

namespace GasFireMonitoringServer.Models.DTOs.Responses
{
    /// <summary>
    /// Response DTO for dashboard summaries and site status
    /// </summary>
    public class SiteStatusDto
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string County { get; set; } = "";
        public string OverallStatus { get; set; } = ""; // "normal", "alarm", "error", "offline"

        // Sensor counts by status
        public int TotalSensors { get; set; }
        public int NormalSensors { get; set; }
        public int AlarmSensors { get; set; }
        public int ErrorSensors { get; set; }
        public int OfflineSensors { get; set; }

        // Sensor types breakdown
        public List<SensorTypeCountDto> SensorTypeBreakdown { get; set; } = new();

        // Recent activity
        public DateTime LastSensorUpdate { get; set; }
        public DateTime LastAlarmTime { get; set; }
        public int ActiveAlarmsCount { get; set; }
        public int TodayAlarmsCount { get; set; }

        // Calculated properties for dashboard display
        public double NormalPercentage => TotalSensors > 0 ? (double)NormalSensors / TotalSensors * 100 : 0;
        public double AlarmPercentage => TotalSensors > 0 ? (double)AlarmSensors / TotalSensors * 100 : 0;
        public double ErrorPercentage => TotalSensors > 0 ? (double)ErrorSensors / TotalSensors * 100 : 0;
        public bool IsHealthy => AlarmSensors == 0 && ErrorSensors == 0;
        public bool IsOnline => OverallStatus != "offline";
        public string StatusIconClass => OverallStatus switch
        {
            "normal" => "status-normal",
            "alarm" => "status-alarm",
            "error" => "status-error",
            "offline" => "status-offline",
            _ => "status-unknown"
        };
    }

    /// <summary>
    /// Sensor type count breakdown for dashboard
    /// </summary>
    public class SensorTypeCountDto
    {
        public string DetectorTypeName { get; set; } = "";
        public int Count { get; set; }
        public int NormalCount { get; set; }
        public int AlarmCount { get; set; }
        public int ErrorCount { get; set; }
    }
}