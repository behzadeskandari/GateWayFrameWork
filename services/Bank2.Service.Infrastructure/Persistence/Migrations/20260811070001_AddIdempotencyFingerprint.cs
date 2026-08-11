using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank2.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                table: "idempotency_records",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "idempotency_records",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "idempotency_records",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "status",
                table: "idempotency_records");

            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "idempotency_records",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
