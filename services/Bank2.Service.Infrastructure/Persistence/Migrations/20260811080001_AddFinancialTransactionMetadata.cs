using System;
using Bank2.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank2.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Bank2DbContext))]
    [Migration("20260811080001_AddFinancialTransactionMetadata")]
    public partial class AddFinancialTransactionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_reference_id",
                table: "transfers",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "transfers",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "transfers",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "transfers",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_reference_id",
                table: "payments",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "payments",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "payments",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "payments",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfers_bank_reference_id",
                table: "transfers",
                column: "bank_reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_status",
                table: "transfers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payments_bank_reference_id",
                table: "payments",
                column: "bank_reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                table: "payments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transfers_bank_reference_id",
                table: "transfers");

            migrationBuilder.DropIndex(
                name: "ix_transfers_status",
                table: "transfers");

            migrationBuilder.DropIndex(
                name: "ix_payments_bank_reference_id",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payments_status",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "bank_reference_id",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "bank_reference_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "payments");
        }
    }
}
