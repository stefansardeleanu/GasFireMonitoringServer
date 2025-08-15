// File: Data/ApplicationDbContext.cs
// This class manages the connection to MariaDB
// UPDATED: Added comprehensive performance indexes for Phase 7.3

using Microsoft.EntityFrameworkCore;  // Entity Framework Core
using GasFireMonitoringServer.Models.Entities;

namespace GasFireMonitoringServer.Data
{
    // DbContext is like a PLC driver - it handles communication with the database
    public class ApplicationDbContext : DbContext
    {
        // Constructor - runs when creating a new instance
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)  // Pass options to parent class
        {
        }

        // DbSet properties - each one represents a table in the database
        // Think of these as PLC data blocks that get saved to memory

        public DbSet<Sensor> Sensors { get; set; }  // Table: Sensors
        public DbSet<Alarm> Alarms { get; set; }    // Table: Alarms
        public DbSet<User> Users { get; set; }      // Table: Users (NEW for authentication)

        // This method configures how entities map to database tables
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Sensor table
            modelBuilder.Entity<Sensor>(entity =>
            {
                // Set the table name
                entity.ToTable("sensor_last_values");

                // Configure primary key
                entity.HasKey(e => e.Id);

                // Configure properties with proper constraints
                entity.Property(e => e.SiteName).HasMaxLength(100);
                entity.Property(e => e.TagName).HasMaxLength(50);
                entity.Property(e => e.Units).HasMaxLength(10);
                entity.Property(e => e.ChannelId).HasMaxLength(10);
                entity.Property(e => e.StatusText).HasMaxLength(100);
                entity.Property(e => e.Topic).HasMaxLength(200);
                entity.Property(e => e.RawJson).HasColumnType("TEXT");

                // ===== PERFORMANCE INDEXES FOR SENSOR QUERIES =====

                // Basic site filtering - MOST USED QUERY PATTERN
                // Supports: WHERE s.SiteId = @siteId ORDER BY s.ChannelId
                entity.HasIndex(e => new { e.SiteId, e.ChannelId })
                    .HasDatabaseName("IX_Sensors_SiteId_ChannelId");

                // Status filtering for alarm queries
                // Supports: WHERE s.Status IN (1, 2) ORDER BY s.SiteId, s.ChannelId
                entity.HasIndex(e => new { e.Status, e.SiteId, e.ChannelId })
                    .HasDatabaseName("IX_Sensors_Status_SiteId_ChannelId");

                // Last update time queries for connectivity status
                // Supports: ORDER BY s.LastUpdated DESC, filtering by update time
                entity.HasIndex(e => e.LastUpdated)
                    .HasDatabaseName("IX_Sensors_LastUpdated");

                // Site status aggregation queries
                // Supports: GROUP BY SiteId with COUNT operations
                entity.HasIndex(e => new { e.SiteId, e.Status })
                    .HasDatabaseName("IX_Sensors_SiteId_Status");

                // Tag-based lookups for specific sensor queries
                // Supports: WHERE s.TagName = @tagName
                entity.HasIndex(e => e.TagName)
                    .HasDatabaseName("IX_Sensors_TagName");

                // Detector type filtering for dashboard queries
                // Supports: WHERE s.DetectorType = @type AND s.SiteId = @siteId
                entity.HasIndex(e => new { e.DetectorType, e.SiteId })
                    .HasDatabaseName("IX_Sensors_DetectorType_SiteId");

                // Keep original basic index for backward compatibility
                entity.HasIndex(e => e.SiteId)
                    .HasDatabaseName("IX_Sensors_SiteId_Basic");
            });

            // Configure Alarm table
            modelBuilder.Entity<Alarm>(entity =>
            {
                // Set the table name
                entity.ToTable("alarms");

                // Configure primary key
                entity.HasKey(e => e.Id);

                // Configure properties with proper constraints
                entity.Property(e => e.SiteName).HasMaxLength(100);
                entity.Property(e => e.SensorTag).HasMaxLength(50);
                entity.Property(e => e.AlarmMessage).HasMaxLength(200);
                entity.Property(e => e.RawMessage).HasMaxLength(500);

                // ===== PERFORMANCE INDEXES FOR ALARM QUERIES =====

                // Most common alarm query pattern: site + time range
                // Supports: WHERE a.SiteId = @siteId AND a.Timestamp >= @start AND a.Timestamp <= @end ORDER BY a.Timestamp DESC
                entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                    .HasDatabaseName("IX_Alarms_SiteId_Timestamp")
                    .IsDescending(false, true); // SiteId ASC, Timestamp DESC for ORDER BY performance

                // Time-based queries for dashboard statistics
                // Supports: WHERE a.Timestamp >= @date ORDER BY a.Timestamp DESC
                entity.HasIndex(e => e.Timestamp)
                    .HasDatabaseName("IX_Alarms_Timestamp")
                    .IsDescending(true); // DESC for most recent first queries

                // Sensor-specific alarm history
                // Supports: WHERE a.SensorTag = @tag ORDER BY a.Timestamp DESC
                entity.HasIndex(e => new { e.SensorTag, e.Timestamp })
                    .HasDatabaseName("IX_Alarms_SensorTag_Timestamp")
                    .IsDescending(false, true); // SensorTag ASC, Timestamp DESC

                // Alarm message frequency analysis
                // Supports: GROUP BY a.AlarmMessage with COUNT operations
                entity.HasIndex(e => e.AlarmMessage)
                    .HasDatabaseName("IX_Alarms_AlarmMessage");

                // Site name filtering (used in some dashboard queries)
                // Supports: WHERE a.SiteName = @siteName
                entity.HasIndex(e => e.SiteName)
                    .HasDatabaseName("IX_Alarms_SiteName");

                // Combined index for complex filtering with limits
                // Supports: WHERE a.SiteId = @siteId AND a.Timestamp >= @start ORDER BY a.Timestamp DESC LIMIT @limit
                entity.HasIndex(e => new { e.SiteId, e.Timestamp, e.Id })
                    .HasDatabaseName("IX_Alarms_SiteId_Timestamp_Id")
                    .IsDescending(false, true, false); // Includes Id for stable sorting

                // Keep original indexes for backward compatibility
                entity.HasIndex(e => e.SiteId)
                    .HasDatabaseName("IX_Alarms_SiteId_Basic");
            });

            // Configure User table (for authentication)
            modelBuilder.Entity<User>(entity =>
            {
                // Set the table name
                entity.ToTable("Users");

                // Configure primary key
                entity.HasKey(e => e.Id);

                // Configure properties with constraints
                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.DisplayName)
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.Property(e => e.AllowedCountiesJson)
                    .HasColumnName("allowed_counties")
                    .HasDefaultValue("[]");

                entity.Property(e => e.AllowedSitesJson)
                    .HasColumnName("allowed_sites")
                    .HasDefaultValue("[]");

                entity.Property(e => e.PermissionsJson)
                    .HasColumnName("permissions")
                    .HasDefaultValue("[]");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                entity.Property(e => e.FailedLoginAttempts)
                    .HasDefaultValue(0);

                // ===== PERFORMANCE INDEXES FOR USER QUERIES =====

                // Login queries - CRITICAL for authentication performance
                // Supports: WHERE u.Username = @username AND u.IsActive = true
                entity.HasIndex(e => new { e.Username, e.IsActive })
                    .HasDatabaseName("IX_Users_Username_IsActive")
                    .IsUnique(); // Username must be unique

                // Role-based queries for authorization
                // Supports: WHERE u.Role = @role AND u.IsActive = true ORDER BY u.Username
                entity.HasIndex(e => new { e.Role, e.IsActive, e.Username })
                    .HasDatabaseName("IX_Users_Role_IsActive_Username");

                // Account security and audit queries
                // Supports: WHERE u.LastLoginAt >= @date ORDER BY u.LastLoginAt DESC
                entity.HasIndex(e => e.LastLoginAt)
                    .HasDatabaseName("IX_Users_LastLoginAt")
                    .IsDescending(true);

                // User management and reporting queries
                // Supports: WHERE u.CreatedAt >= @date ORDER BY u.CreatedAt DESC
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Users_CreatedAt")
                    .IsDescending(true);

                // Failed login tracking for security
                // Supports: WHERE u.FailedLoginAttempts > 0 AND u.IsActive = true
                entity.HasIndex(e => new { e.FailedLoginAttempts, e.IsActive })
                    .HasDatabaseName("IX_Users_FailedLoginAttempts_IsActive");

                // Account status queries
                // Supports: WHERE u.IsActive = @active ORDER BY u.Username
                entity.HasIndex(e => new { e.IsActive, e.Username })
                    .HasDatabaseName("IX_Users_IsActive_Username");

                // Keep original basic indexes for backward compatibility
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Username_Basic");

                entity.HasIndex(e => e.Role)
                    .HasDatabaseName("IX_Users_Role_Basic");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_Users_IsActive_Basic");
            });
        }
    }
}