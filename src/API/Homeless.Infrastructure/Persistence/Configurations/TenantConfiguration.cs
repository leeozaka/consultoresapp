using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;

namespace Homeless.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : BaseEntityConfiguration<Tenant>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.CustomDomain).IsUnique().HasFilter("\"CustomDomain\" IS NOT NULL");

        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(63).IsRequired();
        builder.Property(t => t.CustomDomain).HasMaxLength(253);
        builder.Property(t => t.FrontendOrigin).HasMaxLength(512);
        builder.Property(t => t.ContactEmail).HasMaxLength(254).IsRequired();
        builder.Property(t => t.ContactPhone).HasMaxLength(30);
        builder.Property(t => t.PlanId).IsRequired(false);
        builder.Property(t => t.OwnerUserId).IsRequired(false);
        builder.HasIndex(t => t.OwnerUserId).HasFilter("\"OwnerUserId\" IS NOT NULL");

        builder.Property(t => t.StripeCustomerId).HasMaxLength(255).IsRequired(false);
        builder.Property(t => t.StripeSubscriptionId).HasMaxLength(255).IsRequired(false);
        builder.Property(t => t.PaymentStatus).HasMaxLength(20).HasDefaultValue("none").IsRequired();
        builder.Property(t => t.LastPaymentDate).IsRequired(false);
        builder.Property(t => t.NextBillingDate).IsRequired(false);

        builder.Property(t => t.Status)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<TenantStatus>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<TenantType>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        // JSONB columns
        builder.Property(t => t.Branding)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<BrandingConfig>(v, (JsonSerializerOptions?)null) ?? new BrandingConfig());

        builder.Property(t => t.Entitlements)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

        builder.Property(t => t.Entitlements).Metadata.SetValueComparer(
            new ValueComparer<Dictionary<string, object>>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) ==
                           JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    (JsonSerializerOptions?)null)!));
    }
}
