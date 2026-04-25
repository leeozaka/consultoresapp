using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Domain.Entities;

namespace Homeless.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base configuration for all tenant-scoped entities.
/// Adds standard TenantId column and index on top of BaseEntityConfiguration.
/// </summary>
public abstract class BaseTenantEntityConfiguration<T> : BaseEntityConfiguration<T>
    where T : TenantEntity
{
    protected override void ConfigureEntity(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);

        ConfigureTenantEntity(builder);
    }

    protected abstract void ConfigureTenantEntity(EntityTypeBuilder<T> builder);
}
