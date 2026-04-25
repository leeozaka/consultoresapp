using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Application.Sagas.Payment;

namespace Homeless.Infrastructure.Persistence.Configurations;

public sealed class PaymentSagaStateConfiguration : IEntityTypeConfiguration<PaymentSagaState>
{
    public void Configure(EntityTypeBuilder<PaymentSagaState> builder)
    {
        builder.ToTable("payment_saga_states");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.StripeSessionId).HasMaxLength(255);
        builder.Property(x => x.PaymentIntentId).HasMaxLength(255);
        builder.Property(x => x.SessionUrl).HasMaxLength(2048);
        builder.Property(x => x.FailureReason).HasMaxLength(1024);
    }
}
