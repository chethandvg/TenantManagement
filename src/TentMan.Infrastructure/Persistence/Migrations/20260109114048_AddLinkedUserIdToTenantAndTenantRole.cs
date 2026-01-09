using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TentMan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedUserIdToTenantAndTenantRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedUserId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_LinkedUserId",
                table: "Tenants",
                column: "LinkedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Users_LinkedUserId",
                table: "Tenants",
                column: "LinkedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Users_LinkedUserId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_LinkedUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LinkedUserId",
                table: "Tenants");
        }
    }
}
