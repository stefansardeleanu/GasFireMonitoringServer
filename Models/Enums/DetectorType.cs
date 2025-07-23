// File: Models/Enums/DetectorType.cs

namespace GasFireMonitoringServer.Models.Enums
{
    public enum DetectorType
    {
        Unknown = 0,
        Gas = 1,          // Detects gas leaks (shortened name)
        Flame = 2,        // Detects flames
        ManualCall = 3,   // Manual alarm button
        Smoke = 4         // Detects smoke
    }
}