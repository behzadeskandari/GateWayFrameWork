using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank2.Service.Infrastructure.Persistence.AuditMigrations
{
    /// <inheritdoc />
    public partial class InitialAuditCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    service_name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    operation = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    correlation_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    trace_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    request_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    resource_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    resource_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    actor_subject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    success = table.Column<bool>(type: "INTEGER", nullable: false),
                    error_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    metadata_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_correlation_id",
                table: "audit_records",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_timestamp",
                table: "audit_records",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "audit_records");
        }
    }
}
