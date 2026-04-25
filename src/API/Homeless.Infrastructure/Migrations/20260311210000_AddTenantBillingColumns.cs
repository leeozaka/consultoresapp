using System;
using Homeless.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260311210000_AddTenantBillingColumns")]
public partial class AddTenantBillingColumns : Migration
{
    private const string TableName = "tenants";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StripeCustomerId",
            table: TableName,
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StripeSubscriptionId",
            table: TableName,
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PaymentStatus",
            table: TableName,
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "none");

        migrationBuilder.AddColumn<DateTime>(
            name: "LastPaymentDate",
            table: TableName,
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextBillingDate",
            table: TableName,
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "StripeCustomerId", table: TableName);
        migrationBuilder.DropColumn(name: "StripeSubscriptionId", table: TableName);
        migrationBuilder.DropColumn(name: "PaymentStatus", table: TableName);
        migrationBuilder.DropColumn(name: "LastPaymentDate", table: TableName);
        migrationBuilder.DropColumn(name: "NextBillingDate", table: TableName);
    }
}
