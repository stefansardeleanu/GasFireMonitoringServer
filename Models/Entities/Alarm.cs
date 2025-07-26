// File: Models/Entities/Alarm.cs
// Simplified alarm entity that stores only what comes from MQTT

// File: Models/Entities/Alarm.cs
// Alarm entity that stores both processed and raw alarm data

using System;

namespace GasFireMonitoringServer.Models.Entities
{
    public class Alarm
    {
        // Primary key - auto-incremented by the database
        public int Id { get; set; }

        // Site information
        public int SiteId { get; set; }
        public string SiteName { get; set; } = "";

        // Sensor that triggered the alarm
        public string SensorTag { get; set; } = "";

        // The alarm description (middle part of the message - e.g. "Alarm Level 2", "Detector Fault", etc.)
        public string AlarmMessage { get; set; } = "";

        // The complete raw message from MQTT for reference
        public string RawMessage { get; set; } = "";

        // When the alarm occurred
        public DateTime Timestamp { get; set; }
    }
}