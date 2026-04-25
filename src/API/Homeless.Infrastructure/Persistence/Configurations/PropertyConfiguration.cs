using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;

namespace Homeless.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : BaseTenantEntityConfiguration<Property>
{
    protected override void ConfigureTenantEntity(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");

        // Standard filterable columns — individual indexes
        builder.HasIndex(p => p.City);
        builder.HasIndex(p => p.Bedrooms);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PropertyType);
        builder.HasIndex(p => p.ListingType);

        // Composite for tenant-scoped status-filtered listing queries
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.City, p.Status });

        builder.Property(p => p.ContactPhone).HasMaxLength(30);
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.OwnsOne(p => p.Price, money =>
        {
            money.Property(m => m.AmountInCents)
                .HasColumnName("price_cents")
                .IsRequired();
            money.HasIndex(m => m.AmountInCents);
            money.OwnsOne(m => m.Currency, curr =>
                curr.Property(c => c.Code)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired());
        });
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.State).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Country).HasMaxLength(2).IsRequired();
        builder.Property(p => p.ZipCode).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(300);
        builder.Property(p => p.AreaSqMeters).HasPrecision(10, 2);

        builder.Property(p => p.Status)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PropertyStatus>(v, true))
            .HasMaxLength(20).IsRequired();

        builder.Property(p => p.PropertyType)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PropertyType>(v, true))
            .HasMaxLength(20).IsRequired();

        builder.Property(p => p.ListingType)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<ListingType>(v, true))
            .HasMaxLength(10).IsRequired();

        // JSONB: flexible attributes with GIN index for fast @> queries
        builder.Property(p => p.Attributes)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

        builder.Property(p => p.Attributes).Metadata.SetValueComparer(
            new ValueComparer<Dictionary<string, object>>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) ==
                           JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    (JsonSerializerOptions?)null)!));

        builder.HasIndex(p => p.Attributes)
            .HasMethod("gin")
            .HasAnnotation("Npgsql:IndexMethod", "gin");

        // JSONB: images array
        builder.Property(p => p.Images)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<PropertyImage>>(v, (JsonSerializerOptions?)null) ?? new List<PropertyImage>());

        builder.Property(p => p.Images).Metadata.SetValueComparer(
            new ValueComparer<List<PropertyImage>>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) ==
                           JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => JsonSerializer.Deserialize<List<PropertyImage>>(
                    JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    (JsonSerializerOptions?)null)!));
    }
}
