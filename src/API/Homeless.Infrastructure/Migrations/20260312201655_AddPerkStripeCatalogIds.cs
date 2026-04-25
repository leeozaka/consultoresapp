using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerkStripeCatalogIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripeFeatureId",
                table: "perk_catalog",
                newName: "StripeProductId");

            migrationBuilder.Sql("UPDATE perk_catalog SET \"StripeProductId\" = NULL;");

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "perk_catalog",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "perk_catalog");

            migrationBuilder.RenameColumn(
                name: "StripeProductId",
                table: "perk_catalog",
                newName: "StripeFeatureId");
        }
    }
}
