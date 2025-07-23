// File: Models/Enums/SensorStatus.cs
// An enum is like a PLC ENUM - a list of named constants

namespace GasFireMonitoringServer.Models.Enums
{
    // This enum defines all possible sensor states
    // The numbers match what your PLC sends
    public enum SensorStatus
    {
        // Each line defines a possible status
        // Format: Name = Value

        Normal = 0,           // Sensor is operating normally
        AlarmLevel1 = 1,      // Low-level alarm
        AlarmLevel2 = 2,      // High-level alarm  
        DetectorError = 3,    // Sensor malfunction
        LineOpenFault = 4,    // Wire disconnected
        LineShortFault = 5,   // Wire shorted
        DetectorDisabled = 6  // Sensor turned off
    }
}