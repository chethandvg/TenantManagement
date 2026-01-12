using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultLeaseBillingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add default LeaseBillingSetting for existing leases that don't have one
            // This ensures backward compatibility when deploying the billing engine
            migrationBuilder.Sql(@"
                INSERT INTO LeaseBillingSettings (Id, LeaseId, BillingDay, PaymentTermDays, GenerateInvoiceAutomatically, ProrationMethod, CreatedAtUtc, CreatedBy, IsDeleted)
                SELECT 
                    NEWID(),
                    l.Id,
                    1,                      -- BillingDay: 1st of month (default)
                    0,                      -- PaymentTermDays: Due on invoice date (default)
                    1,                      -- GenerateInvoiceAutomatically: true (default)
                    1,                      -- ProrationMethod: ActualDaysInMonth (default, enum value 1)
                    GETUTCDATE(),
                    'Migration',
                    0                       -- IsDeleted: false
                FROM Leases l
                LEFT JOIN LeaseBillingSettings lbs ON l.Id = lbs.LeaseId
                WHERE lbs.Id IS NULL AND l.IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove LeaseBillingSettings created by this migration
            // Identify by CreatedBy = 'Migration'
            migrationBuilder.Sql(@"
                DELETE FROM LeaseBillingSettings 
                WHERE CreatedBy = 'Migration';
            ");
        }
    }
}
