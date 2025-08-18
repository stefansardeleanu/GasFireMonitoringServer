# Gas Fire Monitoring Server

> Professional ASP.NET Core Web API for industrial gas and fire monitoring systems with real-time MQTT integration, comprehensive authentication, and advanced layout management.

## 🚀 Features

### **🔐 Enterprise Authentication & Authorization**
- **JWT-based Authentication** with configurable expiration
- **Role-based Access Control** (CEO, Regional, Operator)
- **Permission-based Authorization** for granular access control
- **Secure Password Hashing** with BCrypt

### **📊 Real-time Monitoring**
- **MQTT Integration** for real-time sensor data
- **SignalR Real-time Updates** to connected clients
- **Multi-detector Support** (Gas, Fire, Smoke, Heat)
- **Live Status Tracking** with automatic offline detection

### **🏭 Industrial Site Management**
- **Multi-site Monitoring** across Romanian counties (Prahova, Gorj)
- **Site Status Aggregation** with real-time health metrics
- **Sensor Statistics** and performance tracking
- **Geographic Positioning** with percentage-based coordinates

### **🗺️ Advanced Layout System**
- **SVG Layout Management** for precise sensor positioning
- **Grid Layout Fallback** for sites without custom layouts
- **Real-time Layout Updates** via API endpoints
- **File Upload Support** for new site layouts

### **⚙️ Configuration Management**
- **JSON-based Configuration** for easy maintenance
- **Hot Configuration Reload** without service restart
- **Configuration Validation** with comprehensive error checking
- **Backup and Restore** capabilities

### **🚨 Comprehensive Alarm System**
- **Multi-level Alarms** (Level 1: Warning, Level 2: Critical)
- **Alarm Filtering** with date ranges and site-specific queries
- **Alarm Statistics** and trend analysis
- **Real-time Alarm Broadcasting** via SignalR

## 🏗️ Architecture

### **Clean Architecture Pattern**
```
Controllers → Services → Repositories → Database
     ↓           ↓           ↓
   DTOs      Business    Data Access
            Logic       Layer
```

### **Key Components**
- **Controllers**: REST API endpoints with comprehensive documentation
- **Services**: Business logic layer with dependency injection
- **Repositories**: Data access layer with Entity Framework Core
- **DTOs**: Data transfer objects for API communication
- **Middleware**: Global error handling, logging, and performance monitoring

## 📋 API Documentation

### **Interactive API Documentation**
- **Swagger UI**: Available at `/swagger` endpoint
- **Grouped Endpoints**: Organized by functionality with emoji icons
- **Authentication Support**: JWT Bearer token integration
- **Response Examples**: Comprehensive examples for all endpoints

### **API Endpoint Categories**

#### **🔐 Authentication**
- `POST /api/auth/login` - User authentication with JWT token generation

#### **🏭 Sites & Locations**
- `GET /api/site` - Get all industrial sites with status
- `GET /api/site/{id}` - Get specific site details
- `GET /api/site/status-summary` - System-wide status overview

#### **📊 Sensors & Monitoring**
- `GET /api/sensor` - Get all sensors across all sites
- `GET /api/sensor/site/{siteId}` - Get sensors for specific site
- `GET /api/sensor/statistics` - Sensor performance statistics

#### **🚨 Alarms & Alerts**
- `GET /api/alarm` - Get alarms with filtering options
- `GET /api/alarm/site/{siteId}` - Get site-specific alarms
- `GET /api/alarm/active` - Get currently active alarms

#### **⚙️ Configuration**
- `GET /api/configuration/sites` - Get site configurations
- `PUT /api/configuration/sites/{id}` - Update site configuration
- `GET /api/configuration/sensors/site/{siteId}` - Get sensor configurations

#### **🗺️ Layout Management**
- `GET /api/layout/site/{siteId}` - Get site layout information
- `GET /api/layout/site/{siteId}/svg` - Get SVG layout content
- `POST /api/layout/site/{siteId}` - Upload new site layout
- `PUT /api/layout/site/{siteId}/sensors` - Update sensor positions

## 🚀 Getting Started

### **Prerequisites**
- .NET 9.0 SDK
- MariaDB/MySQL Database
- MQTT Broker (for real-time data)
- Visual Studio 2022 or VS Code

### **Installation**

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-repo/GasFireMonitoringServer.git
   cd GasFireMonitoringServer
   ```

2. **Configure the database**
   ```bash
   # Update connection string in appsettings.json
   dotnet ef database update
   ```

3. **Configure application settings**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=GasFireMonitoring;Uid=your_user;Pwd=your_password;"
     },
     "JwtSettings": {
       "Secret": "your-super-secret-jwt-key-at-least-32-characters-long",
       "Issuer": "GasFireMonitoringServer",
       "Audience": "GasFireMonitoringClient",
       "ExpirationDays": 7
     },
     "MqttSettings": {
       "Server": "your-mqtt-broker",
       "Port": 1883,
       "Username": "mqtt_user",
       "Password": "mqtt_password"
     }
   }
   ```

4. **Configure site and sensor data**
   ```bash
   # Edit configuration files in Configuration/Data/
   # - sites.json: Site definitions and coordinates
   # - sensors.json: Sensor positioning data
   # - counties.json: Administrative organization
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the API documentation**
   - Navigate to `https://localhost:7094/swagger`
   - Test endpoints using the interactive interface

### **Authentication Setup**

1. **Default Administrator Account**
   - Username: `admin`
   - Password: `admin123`
   - Role: `CEO` (full system access)

2. **Get JWT Token**
   ```bash
   curl -X POST "https://localhost:7094/api/auth/login" \
        -H "Content-Type: application/json" \
        -d '{"username": "admin", "password": "admin123"}'
   ```

3. **Use Token in API Calls**
   ```bash
   curl -X GET "https://localhost:7094/api/site" \
        -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

## 📁 Configuration

### **Configuration Files**

#### **sites.json** - Site Definitions
```json
[
  {
    "id": 1,
    "name": "SondaMorEni",
    "county": "Prahova",
    "mapX": 45.2,
    "mapY": 67.8,
    "layoutMode": "svg",
    "layoutFile": "site_1_layout.svg"
  }
]
```

#### **sensors.json** - Sensor Positioning
```json
[
  {
    "siteId": 1,
    "channelId": "CH41",
    "layoutX": 65.5,
    "layoutY": 42.3
  }
]
```

#### **counties.json** - Administrative Organization
```json
[
  {
    "name": "Prahova",
    "sites": [1, 2, 3, 4, 7, 8, 9, 10]
  }
]
```

### **Environment Variables**
```bash
SITES_CONFIG_PATH=/path/to/sites.json
SENSORS_CONFIG_PATH=/path/to/sensors.json
COUNTIES_CONFIG_PATH=/path/to/counties.json
LAYOUTS_PATH=/path/to/layouts/
```

## 🗺️ Layout System

### **SVG Layout Requirements**
- **File Format**: Standard SVG with named elements
- **Coordinate System**: 0-100% for responsive scaling
- **File Storage**: `wwwroot/layouts/` directory
- **Naming Convention**: `site_{siteId}_layout.svg`

### **Layout Modes**
- **SVG Mode**: Custom vector graphics with precise positioning
- **Grid Mode**: Automatic grid arrangement (configurable)
- **Auto Mode**: SVG when available, grid as fallback

### **Sensor Positioning**
- **Coordinate Range**: 0-100% (X and Y axes)
- **Real-time Updates**: Position changes applied immediately
- **Visual Integration**: Sensor status reflected in layout display

## 🔧 Development

### **Project Structure**
```
GasFireMonitoringServer/
├── Controllers/          # REST API controllers
├── Services/             # Business logic services
│   ├── Business/         # Domain-specific services
│   └── Infrastructure/   # Cross-cutting services
├── Repositories/         # Data access layer
├── Models/               # Data models and entities
│   ├── DTOs/            # Data transfer objects
│   ├── Entities/        # Database entities
│   └── Configuration/   # Configuration models
├── Data/                # Database context
├── Middleware/          # Custom middleware
├── Filters/             # API filters and attributes
├── Hubs/                # SignalR hubs
├── Configuration/       # Configuration files
│   └── Data/           # JSON configuration data
└── wwwroot/            # Static files and layouts
    └── layouts/        # SVG layout files
```

### **Adding New Features**

#### **New Sensor Types**
1. Update `DetectorType` enum in models
2. Add type-specific validation rules
3. Update sensor processing logic in services

#### **New Sites**
1. Add site to `sites.json` configuration
2. Create layout file (optional)
3. Configure sensor positions in `sensors.json`

#### **New API Endpoints**
1. Add method to appropriate controller
2. Implement business logic in service layer
3. Add repository methods if needed
4. Update DTOs for request/response data

### **Testing**

#### **Automated API Testing**
```powershell
# Run comprehensive API test suite
.\test-api.ps1

# Test specific environment
.\test-api.ps1 -BaseUrl "http://localhost:5208"
```

#### **Manual Testing**
- Use Swagger UI for interactive testing
- Test authentication flows with different roles
- Verify real-time updates via SignalR
- Test configuration changes and hot reload

## 🚨 Troubleshooting

### **Common Issues**

#### **MQTT Connection Issues**
- Verify broker address and credentials
- Check network connectivity and firewall settings
- Review MQTT logs in application output

#### **Database Issues**
- Verify connection string in `appsettings.json`
- Ensure MariaDB service is running
- Check user permissions for database access

#### **Authentication Issues**
- Verify JWT secret key length (minimum 32 characters)
- Check token expiration settings
- Ensure correct username/password combination

#### **Configuration Issues**
- Validate JSON syntax in configuration files
- Check file permissions and paths
- Review startup logs for configuration errors

#### **Layout Issues**
- Verify SVG file format and structure
- Check file naming convention
- Ensure layout directory exists and is writable

### **Logging and Diagnostics**

#### **Log Locations**
- **Console Output**: Real-time application logs
- **File Logs**: `logs/gasfiremonitoring-YYYY-MM-DD.log`
- **Performance Logs**: Slow request detection (>1000ms)

#### **Health Checks**
- **Health Endpoint**: `/health` - Overall system status
- **Ready Endpoint**: `/health/ready` - Service readiness

#### **Performance Monitoring**
- Request/response time tracking
- Database query performance
- MQTT message processing rates
- SignalR connection metrics

## 🔒 Security

### **Security Features**
- JWT token-based authentication
- Password hashing with BCrypt
- Role-based authorization
- CORS configuration for client applications
- Input validation with FluentValidation
- Global error handling with sanitized responses

### **Security Best Practices**
- Use strong passwords for all accounts
- Implement HTTPS in production environments
- Regularly rotate JWT signing keys
- Monitor and log authentication attempts
- Restrict database user permissions
- Keep dependencies updated

## 📈 Performance

### **Performance Optimizations**
- Database query optimization with proper indexing
- Async/await patterns throughout the application
- SignalR connection pooling and efficient broadcasting
- Configuration caching and hot reload
- Performance monitoring and slow request detection

### **Scalability Considerations**
- Stateless architecture for horizontal scaling
- Database connection pooling
- Efficient MQTT message processing
- Optimized SignalR group management
- Memory-efficient data processing

## 🚀 Deployment

### **Production Deployment**

#### **1. Server Requirements**
- Windows Server 2019+ or Linux
- .NET 9.0 Runtime
- MariaDB/MySQL Database
- Reverse proxy (IIS/Nginx) for HTTPS

#### **2. Environment Setup**
```bash
# Set production environment
export ASPNETCORE_ENVIRONMENT=Production

# Configure connection strings
export ConnectionStrings__DefaultConnection="Server=prod-db;Database=GasFireMonitoring;..."

# Set JWT secret from secure storage
export JwtSettings__Secret="your-production-jwt-secret"
```

#### **3. Database Setup**
```bash
# Apply migrations
dotnet ef database update --connection "your-production-connection-string"

# Seed initial data
dotnet run --seed-data
```

#### **4. Service Configuration**
```bash
# Install as Windows Service
dotnet publish -c Release
sc create GasFireMonitoringServer binPath="path\to\published\app.exe"
sc start GasFireMonitoringServer
```

### **Docker Deployment**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "GasFireMonitoringServer.dll"]
```

## 📄 API Testing

A comprehensive PowerShell testing script is included (`test-api.ps1`) that validates all API endpoints:

```powershell
# Run all tests
.\test-api.ps1

# Generates detailed report with:
# - Authentication flow testing
# - All endpoint validation
# - Performance benchmarking
# - Error scenario testing
# - Security validation
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Implement changes following the established architecture
4. Add comprehensive tests
5. Update documentation
6. Submit a pull request

## 📞 Support

For technical support or questions:
- Review the troubleshooting guide above
- Check application logs for error details
- Consult the API documentation at `/swagger`
- Contact system administrator

## 📜 License

[Your License Here]

---

**Built with ❤️ for industrial monitoring excellence**