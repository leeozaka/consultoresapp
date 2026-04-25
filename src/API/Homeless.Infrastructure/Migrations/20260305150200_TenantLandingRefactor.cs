using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantLandingRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrontendOrigin",
                table: "tenants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "agency");

            migrationBuilder.Sql("""
                UPDATE "tenants"
                SET "Type" = 'agency'
                WHERE "Type" IS NULL OR btrim("Type") = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_addons_TenantId",
                table: "tenant_addons",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_addons_TenantId",
                table: "tenant_addons");

            migrationBuilder.DropColumn(
                name: "FrontendOrigin",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "tenants");
        }
    }
}
