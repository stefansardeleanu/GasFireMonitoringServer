# Production Deployment Guide

> Complete guide for deploying the Gas Fire Monitoring Server to production environments.

## 🚀 Overview

This guide covers deploying the Gas Fire Monitoring Server to production, including:
- Server requirements and setup
- Database configuration
- Security hardening
- Performance optimization
- Monitoring and maintenance

## 📋 Prerequisites

### **System Requirements**

#### **Minimum Requirements**
- **OS**: Windows Server 2019+ or Linux (Ubuntu 20.04+, CentOS 8+)
- **RAM**: 4GB minimum, 8GB recommended
- **CPU**: 2 cores minimum, 4 cores recommended
- **Storage**: 50GB minimum, 100GB recommended
- **Network**: Stable internet connection for MQTT

#### **Software Requirements**
- **.NET 9.0 Runtime** (ASP.NET Core Runtime)
- **Database**: MariaDB 10.5+ or MySQL 8.0+
- **Reverse Proxy**: IIS, Nginx, or Apache
- **MQTT Broker**: Mosquitto, HiveMQ, or similar

### **Network Requirements**
- **HTTP/HTTPS**: Ports 80/443 for web access
- **MQTT**: Port 1883 (or 8883 for secure MQTT)
- **Database**: Port 3306 for MariaDB/MySQL
- **SignalR**: WebSocket support through reverse proxy

## 🔧 Production Setup

### **1. Server Preparation**

#### **Windows Server Setup**
```powershell
# Install .NET 9.0 Runtime
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
# Install ASP.NET Core Runtime

# Install IIS and ASP.NET Core Module
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering

# Download and install ASP.NET Core Module for IIS
```

#### **Linux Server Setup (Ubuntu)**
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 9.0 Runtime
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y aspnetcore-runtime-9.0

# Install Nginx
sudo apt install -y nginx

# Install MariaDB
sudo apt install -y mariadb-server mariadb-client
sudo mysql_secure_installation
```

### **2. Database Setup**

#### **MariaDB Configuration**
```sql
-- Create database and user
CREATE DATABASE GasFireMonitoring 
  CHARACTER SET utf8mb4 
  COLLATE utf8mb4_unicode_ci;

-- Create production user with limited privileges
CREATE USER 'gasfiremon'@'localhost' IDENTIFIED BY 'STRONG_RANDOM_PASSWORD';
GRANT SELECT, INSERT, UPDATE, DELETE ON GasFireMonitoring.* TO 'gasfiremon'@'localhost';
FLUSH PRIVILEGES;

-- Configure for production
SET GLOBAL innodb_buffer_pool_size = 1073741824; -- 1GB
SET GLOBAL max_connections = 200;
SET GLOBAL query_cache_size = 67108864; -- 64MB
```

#### **Database Optimization**
```ini
# /etc/mysql/mariadb.conf.d/50-server.cnf
[mysqld]
# Performance Settings
innodb_buffer_pool_size = 1G
innodb_log_file_size = 256M
innodb_flush_log_at_trx_commit = 2
max_connections = 200
query_cache_size = 64M
query_cache_type = 1

# Security Settings
bind-address = 127.0.0.1
skip-name-resolve = 1
sql_mode = STRICT_TRANS_TABLES,NO_ZERO_DATE,NO_ZERO_IN_DATE,ERROR_FOR_DIVISION_BY_ZERO

# Logging
log_error = /var/log/mysql/error.log
slow_query_log = 1
slow_query_log_file = /var/log/mysql/slow.log
long_query_time = 2
```

### **3. Application Deployment**

#### **Build and Publish**
```bash
# Build for production
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Or for Windows
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

#### **File Structure**
```
/opt/gasfiremonitoring/
├── GasFireMonitoringServer.dll
├── appsettings.json
├── appsettings.Production.json
├── Configuration/
│   ├── Data/
│   │   ├── sites.json
│   │   ├── sensors.json
│   │   └── counties.json
│   └── Backups/
├── wwwroot/
│   └── layouts/
├── logs/
└── runtimes/
```

#### **Production Configuration**

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GasFireMonitoring;Uid=gasfiremon;Pwd={SECURE_PASSWORD};"
  },
  "JwtSettings": {
    "Secret": "{SECURE_JWT_SECRET_AT_LEAST_32_CHARS}",
    "Issuer": "GasFireMonitoringServer",
    "Audience": "GasFireMonitoringClient",
    "ExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "ClockSkew": "00:05:00"
  },
  "MqttSettings": {
    "Server": "mqtt-broker.internal",
    "Port": 1883,
    "Username": "prod_mqtt_user",
    "Password": "{SECURE_MQTT_PASSWORD}",
    "ClientId": "GasFireMonitoringServer-Prod",
    "ReconnectDelay": 5,
    "KeepAlivePeriod": 60
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/opt/gasfiremonitoring/logs/application-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithCorrelationId"]
  }
}
```

### **4. Environment Variables**
```bash
# /etc/environment or systemd service file
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:5000
ConnectionStrings__DefaultConnection="Server=localhost;Database=GasFireMonitoring;Uid=gasfiremon;Pwd=SECURE_PASSWORD;"
JwtSettings__Secret="VERY_SECURE_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG"
MqttSettings__Password="SECURE_MQTT_PASSWORD"
SITES_CONFIG_PATH=/opt/gasfiremonitoring/Configuration/Data/sites.json
SENSORS_CONFIG_PATH=/opt/gasfiremonitoring/Configuration/Data/sensors.json
COUNTIES_CONFIG_PATH=/opt/gasfiremonitoring/Configuration/Data/counties.json
LAYOUTS_PATH=/opt/gasfiremonitoring/wwwroot/layouts/
```

## 🔒 Security Configuration

### **SSL/TLS Setup**

#### **Nginx Configuration**
```nginx
# /etc/nginx/sites-available/gasfiremonitoring
server {
    listen 80;
    server_name gasfiremonitoring.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name gasfiremonitoring.yourdomain.com;

    # SSL Configuration
    ssl_certificate /etc/ssl/certs/gasfiremonitoring.crt;
    ssl_certificate_key /etc/ssl/private/gasfiremonitoring.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;

    # Security Headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Proxy Configuration
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering off;
        proxy_read_timeout 100s;
        proxy_connect_timeout 75s;
    }

    # SignalR WebSocket Support
    location /monitoringHub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Static Files
    location /layouts/ {
        proxy_pass http://localhost:5000;
        expires 1d;
        add_header Cache-Control "public, immutable";
    }
}
```

### **Firewall Configuration**
```bash
# Ubuntu UFW
sudo ufw enable
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw allow from 10.0.0.0/8 to any port 3306  # Database (internal network only)
sudo ufw allow from 10.0.0.0/8 to any port 1883  # MQTT (internal network only)
```

### **Application Security**
```json
{
  "SecuritySettings": {
    "RequireHttps": true,
    "EnforceSSL": true,
    "HstsMaxAge": 31536000,
    "ContentSecurityPolicy": "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';",
    "AllowedHosts": ["gasfiremonitoring.yourdomain.com"]
  }
}
```

## ⚡ Performance Optimization

### **Application Performance**
```json
{
  "PerformanceSettings": {
    "EnableResponseCaching": true,
    "EnableResponseCompression": true,
    "MaxConcurrentRequests": 100,
    "DatabaseCommandTimeout": 30,
    "SignalRMaxConnections": 1000,
    "MqttQueueSize": 10000
  }
}
```

### **Database Indexing**
```sql
-- Essential indexes for production performance
CREATE INDEX IX_sensor_last_values_site_id_channel_id ON sensor_last_values(site_id, channel_id);
CREATE INDEX IX_alarms_site_id_timestamp ON alarms(site_id, timestamp DESC);
CREATE INDEX IX_alarms_timestamp ON alarms(timestamp DESC);
CREATE INDEX IX_alarms_alarm_message ON alarms(alarm_message);

-- Additional indexes for specific queries
CREATE INDEX IX_sensor_last_values_timestamp ON sensor_last_values(timestamp DESC);
CREATE INDEX IX_alarms_site_id_status ON alarms(site_id, status);
```

### **Caching Strategy**
```json
{
  "CacheSettings": {
    "ConfigurationCacheDuration": "00:15:00",
    "LayoutCacheDuration": "01:00:00", 
    "SensorDataCacheDuration": "00:01:00",
    "AlarmCacheDuration": "00:05:00",
    "EnableMemoryCache": true,
    "MaxCacheSize": "100MB"
  }
}
```

## 🖥️ Service Management

### **Systemd Service (Linux)**
```ini
# /etc/systemd/system/gasfiremonitoring.service
[Unit]
Description=Gas Fire Monitoring Server
After=network.target mariadb.service

[Service]
Type=exec
User=gasfiremon
Group=gasfiremon
WorkingDirectory=/opt/gasfiremonitoring
ExecStart=/usr/bin/dotnet /opt/gasfiremonitoring/GasFireMonitoringServer.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=gasfiremonitoring
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
EnvironmentFile=-/etc/gasfiremonitoring/environment

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl enable gasfiremonitoring
sudo systemctl start gasfiremonitoring
sudo systemctl status gasfiremonitoring
```

### **Windows Service**
```cmd
# Install as Windows Service
sc create GasFireMonitoringServer binPath="C:\inetpub\gasfiremonitoring\GasFireMonitoringServer.exe" start=auto
sc description GasFireMonitoringServer "Gas Fire Monitoring Server - Industrial Monitoring System"
sc start GasFireMonitoringServer
```

## 📊 Monitoring and Logging

### **Health Monitoring**
```bash
# Health check script
#!/bin/bash
HEALTH_URL="https://gasfiremonitoring.yourdomain.com/health"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $STATUS -eq 200 ]; then
    echo "Service is healthy"
    exit 0
else
    echo "Service is unhealthy (HTTP $STATUS)"
    exit 1
fi
```

### **Log Monitoring**
```bash
# Monitor application logs
tail -f /opt/gasfiremonitoring/logs/application-$(date +%Y%m%d).log

# Monitor error logs specifically
grep -i error /opt/gasfiremonitoring/logs/application-*.log

# Monitor MQTT connections
grep -i mqtt /opt/gasfiremonitoring/logs/application-$(date +%Y%m%d).log
```

### **Performance Monitoring**
```bash
# Monitor system resources
htop
iostat -x 1
netstat -tulpn | grep :5000

# Monitor database connections
mysql -u root -p -e "SHOW PROCESSLIST; SHOW STATUS LIKE 'Threads_%';"
```

## 🔄 Backup and Recovery

### **Database Backup**
```bash
#!/bin/bash
# Daily database backup script
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/backups/database"
mkdir -p $BACKUP_DIR

mysqldump -u root -p GasFireMonitoring > $BACKUP_DIR/gasfiremonitoring_$DATE.sql
gzip $BACKUP_DIR/gasfiremonitoring_$DATE.sql

# Keep last 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

### **Configuration Backup**
```bash
#!/bin/bash
# Configuration backup script
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/backups/configuration"
CONFIG_DIR="/opt/gasfiremonitoring/Configuration"

mkdir -p $BACKUP_DIR
tar -czf $BACKUP_DIR/config_$DATE.tar.gz -C $CONFIG_DIR .

# Keep last 60 days
find $BACKUP_DIR -name "config_*.tar.gz" -mtime +60 -delete
```

### **Full System Backup**
```bash
#!/bin/bash
# Weekly full backup
DATE=$(date +%Y%m%d)
BACKUP_DIR="/opt/backups/full"
APP_DIR="/opt/gasfiremonitoring"

mkdir -p $BACKUP_DIR
tar -czf $BACKUP_DIR/full_backup_$DATE.tar.gz \
    --exclude="$APP_DIR/logs/*" \
    --exclude="$APP_DIR/temp/*" \
    $APP_DIR

# Keep last 4 weeks
find $BACKUP_DIR -name "full_backup_*.tar.gz" -mtime +28 -delete
```

## 🚨 Troubleshooting

### **Common Issues**

#### **Application Won't Start**
```bash
# Check service status
sudo systemctl status gasfiremonitoring

# Check logs
sudo journalctl -u gasfiremonitoring -f

# Check application logs
tail -f /opt/gasfiremonitoring/logs/application-$(date +%Y%m%d).log

# Check permissions
ls -la /opt/gasfiremonitoring/
sudo chown -R gasfiremon:gasfiremon /opt/gasfiremonitoring/
```

#### **Database Connection Issues**
```bash
# Test database connection
mysql -u gasfiremon -p -h localhost GasFireMonitoring

# Check MariaDB status
sudo systemctl status mariadb

# Check database logs
sudo tail -f /var/log/mysql/error.log
```

#### **MQTT Connection Issues**
```bash
# Test MQTT connectivity
mosquitto_sub -h mqtt-broker.internal -t /PLCNEXT/+/+ -u mqtt_user -P mqtt_password

# Check MQTT broker logs
sudo journalctl -u mosquitto -f
```

#### **SSL/Certificate Issues**
```bash
# Test SSL certificate
openssl s_client -connect gasfiremonitoring.yourdomain.com:443

# Check certificate expiration
openssl x509 -in /etc/ssl/certs/gasfiremonitoring.crt -text -noout | grep "Not After"

# Test with curl
curl -I https://gasfiremonitoring.yourdomain.com/health
```

### **Performance Issues**
```bash
# Monitor application performance
curl -s https://gasfiremonitoring.yourdomain.com/health | jq .

# Check database performance
mysql -u root -p -e "SHOW FULL PROCESSLIST; SHOW ENGINE INNODB STATUS\G"

# Monitor system resources
vmstat 1 10
sar -u -r -n DEV 1 10
```

## 📋 Maintenance Tasks

### **Daily Tasks**
- [ ] Check application logs for errors
- [ ] Verify database backup completion
- [ ] Monitor system resource usage
- [ ] Check MQTT connection status

### **Weekly Tasks**
- [ ] Review performance metrics
- [ ] Analyze slow query logs
- [ ] Update system packages
- [ ] Verify configuration backups

### **Monthly Tasks**
- [ ] Review security logs
- [ ] Update SSL certificates if needed
- [ ] Performance optimization review
- [ ] Database maintenance and optimization

### **Quarterly Tasks**
- [ ] Security audit and vulnerability assessment
- [ ] Disaster recovery testing
- [ ] Configuration review and cleanup
- [ ] Performance baseline updates

## 🔄 Updates and Maintenance

### **Application Updates**
```bash
# Backup current version
sudo systemctl stop gasfiremonitoring
cp -r /opt/gasfiremonitoring /opt/backups/app_backup_$(date +%Y%m%d)

# Deploy new version
sudo cp -r /path/to/new/publish/* /opt/gasfiremonitoring/
sudo chown -R gasfiremon:gasfiremon /opt/gasfiremonitoring/

# Update database if needed
cd /opt/gasfiremonitoring
sudo -u gasfiremon dotnet ef database update

# Start service
sudo systemctl start gasfiremonitoring
sudo systemctl status gasfiremonitoring
```

### **Database Maintenance**
```sql
-- Monthly maintenance
OPTIMIZE TABLE sensor_last_values;
OPTIMIZE TABLE alarms;
ANALYZE TABLE sensor_last_values;
ANALYZE TABLE alarms;

-- Check table sizes
SELECT 
  table_name,
  ROUND(((data_length + index_length) / 1024 / 1024), 2) AS "Size (MB)"
FROM information_schema.TABLES 
WHERE table_schema = 'GasFireMonitoring'
ORDER BY (data_length + index_length) DESC;
```

---

**This deployment guide ensures a secure, performant, and maintainable production environment for the Gas Fire Monitoring Server.**