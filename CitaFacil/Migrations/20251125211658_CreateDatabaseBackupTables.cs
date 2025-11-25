using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitaFacil.Migrations
{
    /// <inheritdoc />
    public partial class CreateDatabaseBackupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "database_backup_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    backup_directory = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    auto_backup_enabled = table.Column<bool>(type: "bit", nullable: false),
                    auto_backup_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    retention_days = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    last_backup_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_automatic_backup_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_backup_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "database_backup_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    operation_type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    created_utc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_backup_history", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "database_backup_configs");

            migrationBuilder.DropTable(
                name: "database_backup_history");
        }
    }
}
