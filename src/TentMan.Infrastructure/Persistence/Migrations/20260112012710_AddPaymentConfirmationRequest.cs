using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConfirmationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentConfirmationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProofFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentConfirmationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentConfirmationRequests_Files_ProofFileId",
                        column: x => x.ProofFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaymentConfirmationRequests_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentConfirmationRequests_Leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentConfirmationRequests_Organizations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentConfirmationRequests_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_InvoiceId",
                table: "PaymentConfirmationRequests",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_LeaseId",
                table: "PaymentConfirmationRequests",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_OrgId",
                table: "PaymentConfirmationRequests",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_PaymentDateUtc",
                table: "PaymentConfirmationRequests",
                column: "PaymentDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_PaymentId",
                table: "PaymentConfirmationRequests",
                column: "PaymentId",
                unique: true,
                filter: "[PaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_ProofFileId",
                table: "PaymentConfirmationRequests",
                column: "ProofFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfirmationRequests_Status",
                table: "PaymentConfirmationRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentConfirmationRequests");
        }
    }
}
