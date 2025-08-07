// File: Models/Configuration/SensorConfigurationModel.cs
// Configuration model for sensor layout positioning

namespace GasFireMonitoringServer.Models.Configuration
{
    /// <summary>
    /// Sensor configuration model for layout positioning
    /// Contains only layout coordinates - all other sensor data comes from MQTT
    /// </summary>
    public class SensorConfigurationModel
    {
        /// <summary>
        /// Site ID where this sensor is located
        /// Must match a site ID in sites.json
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Channel ID of the sensor (e.g., "CH41", "CH42")
        /// Must match the channel ID from MQTT messages
        /// </summary>
        public string ChannelId { get; set; } = "";

        /// <summary>
        /// Sensor position on site layout as percentage (0-100)
        /// Used for positioning sensor indicators on SVG layout
        /// </summary>
        public double LayoutX { get; set; }

        /// <summary>
        /// Sensor position on site layout as percentage (0-100)
        /// Used for positioning sensor indicators on SVG layout
        /// </summary>
        public double LayoutY { get; set; }

        /// <summary>
        /// Optional: Sensor display name override
        /// If empty, uses the TagName from MQTT data
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Validate configuration data
        /// </summary>
        public bool IsValid()
        {
            return SiteId > 0
                && !string.IsNullOrWhiteSpace(ChannelId)
                && LayoutX >= 0 && LayoutX <= 100
                && LayoutY >= 0 && LayoutY <= 100;
        }

        /// <summary>
        /// Create a unique key for this sensor
        /// </summary>
        public string GetSensorKey()
        {
            return $"{SiteId}_{ChannelId}";
        }
    }
}