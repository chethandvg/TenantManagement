using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedPaymentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillerId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsumerId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Payments",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepositTransactionId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayName",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayResponse",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionId",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UtilityStatementId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_PaymentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttachments_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAttachments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PaymentStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentStatusHistory_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DepositTransactionId",
                table: "Payments",
                column: "DepositTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayTransactionId",
                table: "Payments",
                column: "GatewayTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrgId_PaymentType_PaymentDateUtc",
                table: "Payments",
                columns: new[] { "OrgId", "PaymentType", "PaymentDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentType",
                table: "Payments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UtilityStatementId",
                table: "Payments",
                column: "UtilityStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttachments_FileId",
                table: "PaymentAttachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttachments_PaymentId",
                table: "PaymentAttachments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttachments_PaymentId_DisplayOrder",
                table: "PaymentAttachments",
                columns: new[] { "PaymentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusHistory_ChangedAtUtc",
                table: "PaymentStatusHistory",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusHistory_PaymentId",
                table: "PaymentStatusHistory",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusHistory_PaymentId_ChangedAtUtc",
                table: "PaymentStatusHistory",
                columns: new[] { "PaymentId", "ChangedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DepositTransactions_DepositTransactionId",
                table: "Payments",
                column: "DepositTransactionId",
                principalTable: "DepositTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_UtilityStatements_UtilityStatementId",
                table: "Payments",
                column: "UtilityStatementId",
                principalTable: "UtilityStatements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DepositTransactions_DepositTransactionId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_UtilityStatements_UtilityStatementId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentAttachments");

            migrationBuilder.DropTable(
                name: "PaymentStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DepositTransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayTransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrgId_PaymentType_PaymentDateUtc",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentType",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UtilityStatementId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BillerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ConsumerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DepositTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayResponse",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UtilityStatementId",
                table: "Payments");
        }
    }
}
