using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSagaState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_saga_states",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckoutKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PerkKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StripeSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SessionUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_saga_states", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_saga_states");
        }
    }
}
