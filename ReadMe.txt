Gas & Fire Monitoring Server

A real-time monitoring system for gas and fire detection sensors across multiple industrial sites in Romania.
Overview

This ASP.NET Core application serves as the backend for a gas and fire monitoring system. It connects to MQTT brokers to receive sensor data, stores it in a MariaDB database, and provides REST APIs and real-time SignalR connections for client applications.
Features

    MQTT Integration: Subscribes to sensor data from PLCNext controllers
    Real-time Updates: SignalR hub for live sensor updates and alarm notifications
    REST API: Comprehensive API for sensor data, alarms, and site information
    Database Storage: Stores sensor readings and alarm history in MariaDB
    Multi-site Support: Monitors 10+ industrial sites across Prahova and Gorj counties

System Architecture

MQTT Broker (Mosquitto)
    ↓ MQTT Messages
C# Server (ASP.NET Core)
    ├── MQTT Client Service
    ├── Data Processing Service
    ├── REST API Controllers
    ├── SignalR Hub
    └── MariaDB Database
    ↓ HTTP/WebSocket
Client Applications

Prerequisites

    .NET 9.0 SDK
    MariaDB 10.5 or higher
    Access to MQTT broker

Installation

    Clone the repository:

bash

git clone [repository-url]
cd GasFireMonitoringServer

    Configure the database connection in appsettings.json:

json

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=plcnext_data;User=your-user;Password=your-password"
  }
}

    Configure MQTT settings in appsettings.json:

json

{
  "MqttSettings": {
    "BrokerAddress": "your-mqtt-broker",
    "Port": 1883,
    "Username": "mqtt-username",
    "Password": "mqtt-password"
  }
}

    Run database migrations (ensure your database tables exist)
    Build and run the application:

bash

dotnet build
dotnet run

API Endpoints
Authentication

    POST /api/auth/login - User login

Sensors

    GET /api/sensor - Get all sensors grouped by site
    GET /api/sensor/site/{siteId} - Get sensors for a specific site
    GET /api/sensor/{id} - Get specific sensor details
    GET /api/sensor/alarms - Get sensors currently in alarm state
    GET /api/sensor/site/{siteId}/stats - Get statistics for a site

Alarms

    GET /api/alarm - Get alarms with filtering options
    GET /api/alarm/site/{siteId} - Get alarms for a specific site
    GET /api/alarm/stats - Get alarm statistics
    GET /api/alarm/active - Get currently active alarms
    POST /api/alarm/{id}/acknowledge - Acknowledge an alarm

Sites

    GET /api/site - Get all sites with status
    GET /api/site/by-county - Get sites grouped by county
    GET /api/site/{id} - Get detailed site information
    GET /api/site/status-summary - Get overall system status

SignalR Hub

Connect to /monitoringHub for real-time updates:

    SubscribeToSites - Subscribe to specific site updates
    SensorUpdate - Receive sensor data updates
    NewAlarm - Receive alarm notifications

MQTT Topic Structure

The system subscribes to topics matching the pattern: /PLCNEXT/+/+

Example topics:

    /PLCNEXT/5_PanouHurezani/CH41 - Sensor data from channel 41
    /PLCNEXT/5_PanouHurezani/Alarms - Alarm messages

Sensor Data Format
json

{
  "rCH41_mA": "5.000000E+00",      // Current (4-20mA)
  "rCH41_PV": "1.250000",          // Process value
  "iCH41_DetStatus": "0",          // Status (0=Normal, 1=Alarm1, 2=Alarm2, etc.)
  "strCH41_TAG": "DET-41",         // Sensor tag name
  "iCH41_DetType": "1"             // Detector type (1=Gas, 2=Flame, etc.)
}

Database Schema
sensor_last_values

    Stores the latest reading from each sensor
    Indexed by site_id and channel_id for fast queries

alarms

    Stores alarm history
    Indexed by site_id and timestamp

Development
Project Structure

GasFireMonitoringServer/
├── Controllers/          # REST API controllers
├── Data/                # Database context and configuration
├── Hubs/                # SignalR hubs
├── Models/              # Data models and entities
├── Services/            # Business logic services
├── Configuration/       # Configuration classes
└── Program.cs          # Application entry point

Adding New Features

    New Sensor Types: Update the DetectorType enum
    New Sites: Add to the _sites list in SiteController
    New API Endpoints: Create new controller methods
    New MQTT Topics: Update the topic pattern in configuration

Monitoring

The application logs important events:

    MQTT connection status
    Sensor updates
    Alarm triggers
    API requests
    Database operations

Security Considerations

    Use strong passwords for database and MQTT
    Implement proper authentication for production
    Use HTTPS in production
    Restrict database user permissions
    Monitor for unusual sensor readings

Troubleshooting
MQTT Connection Issues

    Check broker address and port
    Verify credentials
    Check network connectivity
    Review MQTT broker logs

Database Issues

    Verify connection string
    Check table structure matches entity models
    Ensure MariaDB is running
    Check user permissions

No Sensor Data

    Verify MQTT subscription pattern
    Check if PLCs are publishing data
    Review data processing logs
    Verify JSON parsing logic

License

[Your License Here]
Support

For issues or questions, contact [your contact information]
