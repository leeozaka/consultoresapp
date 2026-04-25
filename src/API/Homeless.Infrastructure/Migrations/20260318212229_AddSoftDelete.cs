using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "tenant_addons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tenant_addons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "perk_catalog",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "perk_catalog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "audit_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "audit_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_IsDeleted",
                table: "tenants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_addons_IsDeleted",
                table: "tenant_addons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_properties_IsDeleted",
                table: "properties",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_plans_IsDeleted",
                table: "plans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_perk_catalog_IsDeleted",
                table: "perk_catalog",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_IsDeleted",
                table: "audit_logs",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_IsDeleted",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_tenant_addons_IsDeleted",
                table: "tenant_addons");

            migrationBuilder.DropIndex(
                name: "IX_properties_IsDeleted",
                table: "properties");

            migrationBuilder.DropIndex(
                name: "IX_plans_IsDeleted",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "IX_perk_catalog_IsDeleted",
                table: "perk_catalog");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_IsDeleted",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "tenant_addons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tenant_addons");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "perk_catalog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "perk_catalog");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "audit_logs");
        }
    }
}
