using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedChargeTypeSystemRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed system-defined charge types
            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "ChargeTypes",
                columns: new[] { "Id", "OrgId", "Code", "Name", "Description", "IsActive", "IsSystemDefined", "IsTaxable", "DefaultAmount", "CreatedAtUtc", "CreatedBy", "ModifiedAtUtc", "ModifiedBy", "IsDeleted", "DeletedAtUtc", "DeletedBy" },
                values: new object[,]
                {
                    { Guid.NewGuid(), null, 1, "Rent", "Monthly rent charge", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 2, "Maintenance", "Maintenance charge", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 3, "Electricity", "Electricity utility charge", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 4, "Water", "Water utility charge", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 5, "Gas", "Gas utility charge", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 6, "Late Fee", "Late payment fee", true, true, false, null, now, "System", null, null, false, null, null },
                    { Guid.NewGuid(), null, 7, "Adjustment", "Manual adjustment (positive or negative)", true, true, false, null, now, "System", null, null, false, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove system-defined charge types
            migrationBuilder.DeleteData(
                table: "ChargeTypes",
                keyColumn: "Code",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7 });
        }
    }
}
