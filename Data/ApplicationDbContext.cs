// File: Data/ApplicationDbContext.cs
// This class manages the connection to MariaDB
// UPDATED: Added User entity for authentication

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

                // Configure properties
                entity.Property(e => e.SiteName).HasMaxLength(100);
                entity.Property(e => e.TagName).HasMaxLength(50);
                entity.Property(e => e.Units).HasMaxLength(10);

                // Create index for faster queries
                entity.HasIndex(e => e.SiteId);
            });

            // Configure Alarm table
            modelBuilder.Entity<Alarm>(entity =>
            {
                // Set the table name
                entity.ToTable("alarms");

                // Configure primary key
                entity.HasKey(e => e.Id);

                // Configure properties
                entity.Property(e => e.SiteName).HasMaxLength(100);
                entity.Property(e => e.SensorTag).HasMaxLength(50);
                entity.Property(e => e.AlarmMessage).HasMaxLength(200);
                entity.Property(e => e.RawMessage).HasMaxLength(500);

                // Create indexes for faster queries
                entity.HasIndex(e => e.SiteId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.SensorTag);
            });

            // Configure User table (NEW for authentication)
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

                // Create indexes for faster queries
                entity.HasIndex(e => e.Username)
                    .IsUnique(); // Username must be unique

                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.LastLoginAt);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}