using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_properties_Price",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "PricePerMonth",
                table: "plans");

            migrationBuilder.RenameColumn(
                name: "PriceAtActivation",
                table: "tenant_addons",
                newName: "price_at_activation");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "tenant_addons",
                newName: "currency_code");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "properties",
                newName: "currency");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "plans",
                newName: "currency_code");

            migrationBuilder.RenameColumn(
                name: "PricePerMonth",
                table: "perk_catalog",
                newName: "price_per_month_cents");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "perk_catalog",
                newName: "currency_code");

            migrationBuilder.AddColumn<long>(
                name: "price_cents",
                table: "properties",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "price_per_month_cents",
                table: "plans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_properties_price_cents",
                table: "properties",
                column: "price_cents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_properties_price_cents",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "price_cents",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "price_per_month_cents",
                table: "plans");

            migrationBuilder.RenameColumn(
                name: "price_at_activation",
                table: "tenant_addons",
                newName: "PriceAtActivation");

            migrationBuilder.RenameColumn(
                name: "currency_code",
                table: "tenant_addons",
                newName: "CurrencyCode");

            migrationBuilder.RenameColumn(
                name: "currency",
                table: "properties",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "currency_code",
                table: "plans",
                newName: "CurrencyCode");

            migrationBuilder.RenameColumn(
                name: "price_per_month_cents",
                table: "perk_catalog",
                newName: "PricePerMonth");

            migrationBuilder.RenameColumn(
                name: "currency_code",
                table: "perk_catalog",
                newName: "CurrencyCode");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "properties",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonth",
                table: "plans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_properties_Price",
                table: "properties",
                column: "Price");
        }
    }
}
