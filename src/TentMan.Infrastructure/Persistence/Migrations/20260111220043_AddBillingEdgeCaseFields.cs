using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingEdgeCaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "UtilityStatements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UtilityStatements",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ProrationMethod",
                table: "LeaseBillingSettings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAtUtc",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "InvoiceLines",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceRefId",
                table: "InvoiceLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityStatements_LeaseId_UtilityType_BillingPeriodStart_BillingPeriodEnd_IsFinal",
                table: "UtilityStatements",
                columns: new[] { "LeaseId", "UtilityType", "BillingPeriodStart", "BillingPeriodEnd", "IsFinal" },
                unique: true,
                filter: "IsFinal = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UtilityStatements_LeaseId_UtilityType_BillingPeriodStart_BillingPeriodEnd_IsFinal",
                table: "UtilityStatements");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "UtilityStatements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UtilityStatements");

            migrationBuilder.DropColumn(
                name: "ProrationMethod",
                table: "LeaseBillingSettings");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidedAtUtc",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "SourceRefId",
                table: "InvoiceLines");
        }
    }
}
