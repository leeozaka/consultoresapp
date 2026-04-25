using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerUserIdToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_OwnerUserId",
                table: "tenants",
                column: "OwnerUserId",
                filter: "\"OwnerUserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_OwnerUserId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "tenants");
        }
    }
}
