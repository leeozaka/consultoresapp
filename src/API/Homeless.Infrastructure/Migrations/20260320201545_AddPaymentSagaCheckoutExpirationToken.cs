using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSagaCheckoutExpirationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CheckoutExpirationTokenId",
                table: "payment_saga_states",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutExpirationTokenId",
                table: "payment_saga_states");
        }
    }
}
