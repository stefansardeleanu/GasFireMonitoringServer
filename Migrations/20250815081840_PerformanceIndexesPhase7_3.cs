using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasFireMonitoringServer.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexesPhase7_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "alarms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    SiteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SensorTag = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlarmMessage = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarms", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_last_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    site_id = table.Column<int>(type: "int", nullable: false),
                    site_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel_id = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tag_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detector_type = table.Column<int>(type: "int", nullable: false),
                    process_value = table.Column<double>(type: "double", nullable: false),
                    current_value = table.Column<double>(type: "double", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    status_text = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    units = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_updated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    raw_json = table.Column<string>(type: "TEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_last_values", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    allowed_counties = table.Column<string>(type: "longtext", nullable: false, defaultValue: "[]")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allowed_sites = table.Column<string>(type: "longtext", nullable: false, defaultValue: "[]")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    permissions = table.Column<string>(type: "longtext", nullable: false, defaultValue: "[]")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP"),
                    last_login_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_AlarmMessage",
                table: "alarms",
                column: "AlarmMessage");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_SensorTag_Timestamp",
                table: "alarms",
                columns: new[] { "SensorTag", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_SiteId_Basic",
                table: "alarms",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_SiteId_Timestamp",
                table: "alarms",
                columns: new[] { "SiteId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_SiteId_Timestamp_Id",
                table: "alarms",
                columns: new[] { "SiteId", "Timestamp", "Id" },
                descending: new[] { false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_SiteName",
                table: "alarms",
                column: "SiteName");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_Timestamp",
                table: "alarms",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_DetectorType_SiteId",
                table: "sensor_last_values",
                columns: new[] { "detector_type", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_LastUpdated",
                table: "sensor_last_values",
                column: "last_updated");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SiteId_Basic",
                table: "sensor_last_values",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SiteId_ChannelId",
                table: "sensor_last_values",
                columns: new[] { "site_id", "channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SiteId_Status",
                table: "sensor_last_values",
                columns: new[] { "site_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Status_SiteId_ChannelId",
                table: "sensor_last_values",
                columns: new[] { "status", "site_id", "channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_TagName",
                table: "sensor_last_values",
                column: "tag_name");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FailedLoginAttempts_IsActive",
                table: "Users",
                columns: new[] { "failed_login_attempts", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Basic",
                table: "Users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Username",
                table: "Users",
                columns: new[] { "is_active", "username" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "last_login_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Basic",
                table: "Users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive_Username",
                table: "Users",
                columns: new[] { "role", "is_active", "username" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username_Basic",
                table: "Users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username_IsActive",
                table: "Users",
                columns: new[] { "username", "is_active" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alarms");

            migrationBuilder.DropTable(
                name: "sensor_last_values");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
