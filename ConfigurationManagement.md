# Configuration Management Guide

> Comprehensive guide for managing site configurations, sensor positions, and layout settings in the Gas Fire Monitoring Server.

## 📋 Overview

The Gas Fire Monitoring Server uses a JSON-based configuration system that allows for:
- **Hot configuration reload** without service restart
- **Real-time updates** via REST API endpoints
- **Automatic backup** before configuration changes
- **Validation** of all configuration data
- **Rollback capabilities** for failed updates

## 📁 Configuration File Structure

### **File Locations**
```
GasFireMonitoringServer/
├── Configuration/
│   └── Data/
│       ├── sites.json          # Site definitions and coordinates
│       ├── sensors.json        # Sensor positioning data
│       └── counties.json       # Administrative organization
└── wwwroot/
    └── layouts/               # SVG layout files
        ├── site_1_layout.svg
        ├── site_2_layout.svg
        └── ...
```

### **Environment Variables** (Optional)
```bash
SITES_CONFIG_PATH=/custom/path/to/sites.json
SENSORS_CONFIG_PATH=/custom/path/to/sensors.json
COUNTIES_CONFIG_PATH=/custom/path/to/counties.json
LAYOUTS_PATH=/custom/path/to/layouts/
```

## 🏭 Site Configuration (sites.json)

### **File Structure**
```json
[
  {
    "id": 1,
    "name": "SondaMorEni",
    "county": "Prahova",
    "mapX": 45.2,
    "mapY": 67.8,
    "layoutMode": "svg",
    "layoutFile": "site_1_layout.svg",
    "gridConfig": {
      "columns": 4,
      "rows": 3,
      "spacing": 10.0,
      "offsetX": 15.0,
      "offsetY": 20.0,
      "groupByType": true,
      "showLabels": true
    }
  },
  {
    "id": 2,
    "name": "SondaArb",
    "county": "Prahova",
    "mapX": 52.1,
    "mapY": 71.4,
    "layoutMode": "grid",
    "layoutFile": null,
    "gridConfig": {
      "columns": 3,
      "rows": 4,
      "spacing": 15.0,
      "offsetX": 10.0,
      "offsetY": 10.0,
      "groupByType": false,
      "showLabels": true
    }
  }
]
```

### **Site Properties**

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `id` | integer | Unique site identifier | Required, positive integer |
| `name` | string | Site display name | Required, 2-100 characters |
| `county` | string | Romanian county name | Required, 2-50 characters |
| `mapX` | number | X coordinate on county map | Required, 0-100% |
| `mapY` | number | Y coordinate on county map | Required, 0-100% |
| `layoutMode` | string | Layout display mode | Required: "svg", "grid", or "auto" |
| `layoutFile` | string | SVG layout filename | Optional, required if layoutMode="svg" |
| `gridConfig` | object | Grid layout configuration | Optional, used when layoutMode="grid" |

### **Grid Configuration Properties**

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `columns` | integer | Number of grid columns | 4 |
| `rows` | integer | Number of grid rows | 3 |
| `spacing` | number | Spacing between sensors (%) | 10.0 |
| `offsetX` | number | Horizontal offset (%) | 15.0 |
| `offsetY` | number | Vertical offset (%) | 20.0 |
| `groupByType` | boolean | Group sensors by detector type | true |
| `showLabels` | boolean | Display sensor labels | true |

### **Layout Modes**

#### **SVG Mode** (`"svg"`)
- Uses custom SVG layout file
- Precise sensor positioning
- Requires `layoutFile` property
- SVG file must exist in `wwwroot/layouts/`

#### **Grid Mode** (`"grid"`)
- Automatic grid-based positioning
- Configurable via `gridConfig` properties
- No SVG file required
- Sensors arranged in uniform grid

#### **Auto Mode** (`"auto"`)
- Uses SVG layout if available
- Falls back to grid layout if no SVG
- Recommended for most sites
- Provides flexibility for future layout additions

## 📡 Sensor Configuration (sensors.json)

### **File Structure**
```json
[
  {
    "siteId": 1,
    "channelId": "CH41",
    "layoutX": 65.5,
    "layoutY": 42.3
  },
  {
    "siteId": 1,
    "channelId": "CH42",
    "layoutX": 32.1,
    "layoutY": 78.9
  },
  {
    "siteId": 2,
    "channelId": "CH43",
    "layoutX": 50.0,
    "layoutY": 25.0
  }
]
```

### **Sensor Properties**

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `siteId` | integer | Associated site ID | Required, must exist in sites.json |
| `channelId` | string | MQTT channel identifier | Required, 2-20 characters |
| `layoutX` | number | X coordinate on site layout | Required, 0-100% |
| `layoutY` | number | Y coordinate on site layout | Required, 0-100% |

### **Coordinate System**
- **Range**: 0-100% for both X and Y coordinates
- **Origin**: Top-left corner (0,0)
- **X-axis**: Left to right (0% = left edge, 100% = right edge)
- **Y-axis**: Top to bottom (0% = top edge, 100% = bottom edge)
- **Responsive**: Scales automatically with client display size

## 🗺️ County Configuration (counties.json)

### **File Structure**
```json
[
  {
    "name": "Prahova",
    "sites": [1, 2, 3, 4, 7, 8, 9, 10]
  },
  {
    "name": "Gorj",
    "sites": [5, 6]
  }
]
```

### **County Properties**

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `name` | string | County name | Required, 2-50 characters |
| `sites` | array | Site IDs in this county | Required, array of integers |

### **Romanian Counties Supported**
- **Prahova**: Primary operational area (8 sites)
- **Gorj**: Secondary operational area (2 sites)
- **Extensible**: Additional counties can be added as needed

## 🔧 API Configuration Management

### **Authentication Required**
All configuration endpoints require JWT authentication with appropriate permissions:
- **ManageConfiguration** permission for updates
- **ViewConfiguration** permission for reading

### **Site Configuration Endpoints**

#### **Get All Site Configurations**
```http
GET /api/configuration/sites
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Site configurations retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "SondaMorEni",
      "county": "Prahova",
      "mapX": 45.2,
      "mapY": 67.8,
      "layoutMode": "svg",
      "layoutFile": "site_1_layout.svg"
    }
  ],
  "count": 1,
  "timestamp": "2025-08-17T12:00:00Z"
}
```

#### **Update Site Configuration**
```http
PUT /api/configuration/sites/1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "name": "SondaMorEni Updated",
  "county": "Prahova",
  "mapX": 47.5,
  "mapY": 69.2,
  "layoutMode": "auto",
  "gridConfig": {
    "columns": 4,
    "rows": 3,
    "spacing": 12.0,
    "offsetX": 15.0,
    "offsetY": 20.0,
    "groupByType": true,
    "showLabels": true
  }
}
```

### **Sensor Configuration Endpoints**

#### **Get Sensor Configuration for Site**
```http
GET /api/configuration/sensors/site/1
Authorization: Bearer {jwt_token}
```

#### **Update Sensor Positions**
```http
PUT /api/configuration/sensors/site/1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "sensors": [
    {
      "channelId": "CH41",
      "layoutX": 70.0,
      "layoutY": 45.0
    },
    {
      "channelId": "CH42",
      "layoutX": 35.0,
      "layoutY": 80.0
    }
  ],
  "backupBefore": true,
  "validatePositions": true
}
```

## 📝 Configuration Validation

### **Automatic Validation**
The system automatically validates all configuration changes:

#### **Site Validation Rules**
- Site ID must be unique across all sites
- Site name must be 2-100 characters
- County must be 2-50 characters
- Map coordinates must be 0-100%
- Layout mode must be "svg", "grid", or "auto"
- If layoutMode="svg", layoutFile must be specified
- Grid configuration values must be within valid ranges

#### **Sensor Validation Rules**
- Site ID must exist in sites.json
- Channel ID must be unique within the site
- Layout coordinates must be 0-100%
- Channel ID must match MQTT topic pattern

#### **County Validation Rules**
- County name must be unique
- Site IDs must exist in sites.json
- No site can belong to multiple counties

### **Validation Error Examples**
```json
{
  "success": false,
  "message": "Configuration validation failed",
  "errors": [
    "Site ID 1 already exists",
    "Map coordinate X (150.5) is out of range (0-100)",
    "Layout file 'missing.svg' does not exist"
  ],
  "timestamp": "2025-08-17T12:00:00Z"
}
```

## 🔄 Configuration Backup and Restore

### **Automatic Backup**
- **Before Updates**: Automatic backup created before any configuration change
- **Backup Location**: `Configuration/Backups/`
- **Filename Format**: `{config-type}_{timestamp}.json`
- **Retention**: 30 days (configurable)

### **Backup Files**
```
Configuration/
├── Backups/
│   ├── sites_2025-08-17_120000.json
│   ├── sensors_2025-08-17_120000.json
│   └── counties_2025-08-17_120000.json
└── Data/
    ├── sites.json
    ├── sensors.json
    └── counties.json
```

### **Manual Backup**
```bash
# Create manual backup
curl -X POST "https://localhost:7094/api/configuration/backup" \
     -H "Authorization: Bearer {jwt_token}"
```

### **Restore from Backup**
```bash
# List available backups
curl -X GET "https://localhost:7094/api/configuration/backups" \
     -H "Authorization: Bearer {jwt_token}"

# Restore specific backup
curl -X POST "https://localhost:7094/api/configuration/restore" \
     -H "Authorization: Bearer {jwt_token}" \
     -H "Content-Type: application/json" \
     -d '{"backupTimestamp": "2025-08-17T12:00:00Z", "configType": "sites"}'
```

## 🚀 Hot Configuration Reload

### **Automatic Reload**
Configuration changes are automatically applied without service restart:

1. **File Change Detection**: System monitors configuration files for changes
2. **Validation**: New configuration is validated before application
3. **Atomic Update**: Configuration is updated atomically to prevent partial states
4. **Client Notification**: Connected clients are notified via SignalR
5. **Rollback**: Failed updates are automatically rolled back

### **Manual Reload**
```bash
# Force configuration reload
curl -X POST "https://localhost:7094/api/configuration/reload" \
     -H "Authorization: Bearer {jwt_token}"
```

### **Reload Status**
```json
{
  "success": true,
  "message": "Configuration reloaded successfully",
  "data": {
    "sitesLoaded": 10,
    "sensorsLoaded": 45,
    "countiesLoaded": 2,
    "layoutsFound": 8,
    "reloadTimestamp": "2025-08-17T12:00:00Z"
  },
  "timestamp": "2025-08-17T12:00:00Z"
}
```

## 🔧 Adding New Sites

### **Step-by-Step Process**

#### **1. Add Site to sites.json**
```json
{
  "id": 11,
  "name": "NewSiteName",
  "county": "Prahova",
  "mapX": 60.0,
  "mapY": 45.0,
  "layoutMode": "auto",
  "layoutFile": null,
  "gridConfig": {
    "columns": 4,
    "rows": 3,
    "spacing": 10.0,
    "offsetX": 15.0,
    "offsetY": 20.0,
    "groupByType": true,
    "showLabels": true
  }
}
```

#### **2. Add Sensors to sensors.json** (if needed)
```json
[
  {
    "siteId": 11,
    "channelId": "CH101",
    "layoutX": 25.0,
    "layoutY": 25.0
  },
  {
    "siteId": 11,
    "channelId": "CH102",
    "layoutX": 75.0,
    "layoutY": 75.0
  }
]
```

#### **3. Update County Assignment** (if needed)
```json
{
  "name": "Prahova",
  "sites": [1, 2, 3, 4, 7, 8, 9, 10, 11]
}
```

#### **4. Create SVG Layout** (optional)
- Create `site_11_layout.svg` in `wwwroot/layouts/`
- Update `layoutMode` to "svg" and set `layoutFile`

#### **5. Validate Configuration**
```bash
curl -X POST "https://localhost:7094/api/configuration/validate" \
     -H "Authorization: Bearer {jwt_token}"
```

## 🚨 Troubleshooting

### **Common Issues**

#### **Configuration Not Loading**
- Check JSON syntax validity
- Verify file permissions
- Review application logs for specific errors
- Ensure environment variables point to correct paths

#### **Validation Errors**
- Review validation error messages in API response
- Check that all required fields are present
- Verify coordinate ranges are 0-100%
- Ensure site IDs are unique

#### **Layout Issues**
- Verify SVG file exists in `wwwroot/layouts/`
- Check SVG file format and structure
- Ensure layout file naming follows convention: `site_{id}_layout.svg`
- Validate that layoutMode matches available layout type

#### **Backup/Restore Issues**
- Check backup directory permissions
- Verify backup file integrity
- Ensure sufficient disk space for backups
- Review restore logs for specific errors

### **Configuration Validation Checklist**

Before applying configuration changes, verify:

- [ ] JSON syntax is valid (use JSON validator)
- [ ] All required fields are present
- [ ] Site IDs are unique and positive integers
- [ ] Coordinates are within 0-100% range
- [ ] County names match existing counties
- [ ] Layout files exist if specified
- [ ] Channel IDs are unique within each site
- [ ] Grid configuration values are reasonable

### **Performance Considerations**

#### **Large Configuration Files**
- Keep sensor count per site reasonable (<100 sensors)
- Use efficient JSON structure (avoid nested arrays)
- Consider configuration file splitting for very large deployments

#### **Frequent Updates**
- Batch multiple configuration changes when possible
- Use validation endpoints before applying changes
- Monitor system performance during configuration updates

## 📊 Configuration Monitoring

### **Configuration Metrics**
Monitor configuration system health using these metrics:

- **Configuration Load Time**: Time to load all configuration files
- **Validation Success Rate**: Percentage of successful validations
- **Hot Reload Frequency**: Number of configuration reloads per hour
- **Backup Creation Success**: Backup operation success rate

### **Configuration Logs**
Key log entries to monitor:

```log
[INFO] Configuration loaded successfully: 10 sites, 45 sensors, 2 counties
[WARN] Configuration validation failed: Site ID 5 coordinates out of range
[ERROR] Failed to create configuration backup: Insufficient disk space
[INFO] Hot reload completed: 8 affected clients notified
```

### **Health Check Integration**
Configuration status is included in system health checks:

```bash
curl -X GET "https://localhost:7094/health"
```

Response includes configuration status:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "configuration": {
      "status": "Healthy",
      "description": "Configuration system operational",
      "data": {
        "sitesLoaded": 10,
        "sensorsLoaded": 45,
        "lastReload": "2025-08-17T12:00:00Z"
      }
    }
  }
}
```

## 🔐 Security Considerations

### **Access Control**
- Configuration endpoints require `ManageConfiguration` permission
- Use role-based access control for configuration management
- Audit all configuration changes with user tracking
- Implement IP whitelisting for configuration access in production

### **Data Protection**
- Store configuration backups securely
- Use HTTPS for all configuration API calls
- Validate all input data to prevent injection attacks
- Implement rate limiting for configuration endpoints

### **Best Practices**
- Test configuration changes in development environment first
- Use version control for configuration file templates
- Implement automated configuration validation in CI/CD pipeline
- Document all configuration changes with business justification

## 📚 Configuration Templates

### **New Site Template**
```json
{
  "id": {{SITE_ID}},
  "name": "{{SITE_NAME}}",
  "county": "{{COUNTY_NAME}}",
  "mapX": {{MAP_X_COORDINATE}},
  "mapY": {{MAP_Y_COORDINATE}},
  "layoutMode": "auto",
  "layoutFile": null,
  "gridConfig": {
    "columns": 4,
    "rows": 3,
    "spacing": 10.0,
    "offsetX": 15.0,
    "offsetY": 20.0,
    "groupByType": true,
    "showLabels": true
  }
}
```

### **Sensor Position Template**
```json
{
  "siteId": {{SITE_ID}},
  "channelId": "{{CHANNEL_ID}}",
  "layoutX": {{X_COORDINATE}},
  "layoutY": {{Y_COORDINATE}}
}
```

### **Environment-Specific Configurations**

#### **Development Configuration**
```json
{
  "ConfigurationSettings": {
    "EnableHotReload": true,
    "ValidationStrictness": "Relaxed",
    "BackupRetentionDays": 7,
    "AutoValidateOnLoad": true
  }
}
```

#### **Production Configuration**
```json
{
  "ConfigurationSettings": {
    "EnableHotReload": false,
    "ValidationStrictness": "Strict",
    "BackupRetentionDays": 30,
    "AutoValidateOnLoad": true,
    "RequireApprovalForChanges": true
  }
}
```

## 🚀 Future Enhancements

### **Planned Features**
- **Configuration Versioning**: Track configuration changes over time
- **A/B Testing**: Test configuration changes with subset of clients
- **Configuration Templates**: Predefined templates for common site types
- **Bulk Operations**: Update multiple sites/sensors simultaneously
- **Configuration Diff**: Compare configuration versions
- **Automated Validation**: Enhanced validation rules with custom logic

### **Integration Possibilities**
- **External Configuration Stores**: Azure App Configuration, AWS Parameter Store
- **Database Storage**: Move from JSON files to database tables
- **Configuration UI**: Web-based configuration management interface
- **Import/Export**: Support for Excel/CSV configuration import
- **Configuration API Gateway**: Centralized configuration for multiple services

---

**For additional support with configuration management, consult the main README.md or contact your system administrator.**