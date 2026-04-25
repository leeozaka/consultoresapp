using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAddonPerkSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "perk_catalog");

            migrationBuilder.DropTable(
                name: "tenant_addons");

            migrationBuilder.DropColumn(
                name: "CheckoutKind",
                table: "payment_saga_states");

            migrationBuilder.DropColumn(
                name: "PerkKey",
                table: "payment_saga_states");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutKind",
                table: "payment_saga_states",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PerkKey",
                table: "payment_saga_states",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "perk_catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StripePriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeProductId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    price_per_month_cents = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perk_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_addons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingCycleStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerkKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    price_at_activation = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_addons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_perk_catalog_IsDeleted",
                table: "perk_catalog",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_perk_catalog_Key",
                table: "perk_catalog",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_addons_IsDeleted",
                table: "tenant_addons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_addons_tenant_perk_status",
                table: "tenant_addons",
                columns: new[] { "TenantId", "PerkKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_addons_TenantId",
                table: "tenant_addons",
                column: "TenantId");
        }
    }
}
