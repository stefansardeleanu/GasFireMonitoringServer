// File: Models/Entities/Sensor.cs
// This represents a sensor in our database - matching your Python structure

using System;  // Gives us access to DateTime and other basic types
using GasFireMonitoringServer.Models.Enums;  // So we can use our enums
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFireMonitoringServer.Models.Entities
{
    [Table("sensor_last_values")]  // Maps to your existing table
    public class Sensor
    {
        // Primary key - unique identifier for each sensor
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // Site information
        [Column("site_id")]
        public int SiteId { get; set; }

        [Column("site_name")]
        public string SiteName { get; set; }

        // Channel number (e.g., "41" from "CH41")
        [Column("channel_id")]
        public string ChannelId { get; set; }

        // Sensor identification
        [Column("tag_name")]
        public string TagName { get; set; }  // e.g., "KGD-002"

        // Sensor type (your detector_type column)
        [Column("detector_type")]
        public int DetectorType { get; set; }

        // Current values
        [Column("process_value")]
        public double ProcessValue { get; set; }  // The measured value

        [Column("current_value")]
        public double CurrentValue { get; set; }  // The 4-20mA signal

        // Status (your status column)
        [Column("status")]
        public int Status { get; set; }

        [Column("status_text")]
        public string StatusText { get; set; }

        // Units of measurement
        [Column("units")]
        public string Units { get; set; }  // e.g., "%LEL", "PPM"

        // Timestamp of last update
        [Column("last_updated")]
        public DateTime LastUpdated { get; set; }

        // Additional columns from your Python database
        [Column("topic")]
        public string Topic { get; set; }

        [Column("raw_json")]
        public string RawJson { get; set; }
    }
}