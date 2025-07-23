using GasFireMonitoringServer.Models.Enums;

namespace GasFireMonitoringServer.Models.Entities
{
    public class Alarm
    {
        // Primary key for the alarm entity auto-incermeented by the database
        public int Id { get; set; }
        // Site information where the alarm is triggered
        public int SiteId { get; set; } // Identifier for the site where the alarm is triggered
        public string SiteName { get; set; } // Name of the site where the alarm is triggered
        //Sensor information that triggered the alarm
        public string SensorTag { get; set; } // Tag name of the sensor that triggered the alarm
        public string ChannelID { get; set; } // Unique channel identifier for the sensor that triggered the alarm
        //Alarm details
        public SensorStatus AlarmType { get; set; } // Type of alarm (e.g., low-level, high-level, error)
        public int AlarmLevel { get; set; } // Level of the alarm, used to categorize severity
        //Value when alarm was triggered
        public double Value { get; set; } // Value of the sensor when the alarm was triggered
        public string units { get; set; } // Units of measurement for the sensor value (e.g., ppm, degrees)
        // Timestamp when the alarm was triggered
        public DateTime Timestamp { get; set; } // Timestamp of when the alarm was triggered
        // Original message from MQTT
        public string RawMessage { get; set; } // Raw message received from MQTT that triggered the alarm

    }
}
