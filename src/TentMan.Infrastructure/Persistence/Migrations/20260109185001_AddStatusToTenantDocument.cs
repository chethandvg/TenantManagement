using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToTenantDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "TenantDocuments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TenantDocuments");
        }
    }
}
