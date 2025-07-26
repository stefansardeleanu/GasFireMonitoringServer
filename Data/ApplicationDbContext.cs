// File: Data/ApplicationDbContext.cs
// This class manages the connection to MariaDB

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
        }
    }
}