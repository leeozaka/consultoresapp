using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Domain.Entities;
using Homeless.Domain.ValueObjects;

namespace Homeless.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : BaseEntityConfiguration<Plan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.OwnsOne(p => p.Price, money =>
        {
            money.Property(m => m.AmountInCents)
                .HasColumnName("price_per_month_cents")
                .IsRequired();
            money.OwnsOne(m => m.Currency, curr =>
                curr.Property(c => c.Code)
                    .HasColumnName("currency_code")
                    .HasMaxLength(3)
                    .IsRequired());
        });
        builder.Property(p => p.StripePriceId).HasMaxLength(255).IsRequired(false);
        builder.Property(p => p.MaxProperties).IsRequired();
        builder.Property(p => p.PortalTheme).HasMaxLength(20).IsRequired().HasDefaultValue("default");
    }
}
