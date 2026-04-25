using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Homeless.Domain.Entities;

namespace Homeless.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CommandName);
        builder.HasIndex(a => a.CreatedAt);

        builder.Property(a => a.CommandName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.CorrelationId).HasMaxLength(50);
        builder.Property(a => a.Result).HasMaxLength(500);
    }
}
