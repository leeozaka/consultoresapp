using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanPortalTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalTheme",
                table: "plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.Sql(
                """
                UPDATE plans SET "PortalTheme" = 'minimal' WHERE "Name" = 'Profissional';
                UPDATE plans SET "PortalTheme" = 'premium' WHERE "Name" = 'Enterprise';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortalTheme",
                table: "plans");
        }
    }
}
