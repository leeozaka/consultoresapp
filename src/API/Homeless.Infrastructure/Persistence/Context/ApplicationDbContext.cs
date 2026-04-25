using System.Linq.Expressions;
using System.Reflection;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Entities;
using Homeless.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Homeless.Infrastructure.Persistence.Context;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext
) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IWriteDbContext
{
    // ── Multi-tenant real estate ───────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── Subscription plans ────────────────────────────────────────────────────
    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PaymentSagaState> PaymentSagaStates => Set<PaymentSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ── Global Query Filters ──────────────────────────────────────────────
        // Two filters applied to all domain entities:
        //   1. Soft-delete: excludes rows where IsDeleted = true.
        //   2. Tenant isolation (ITenantEntity only): restricts to the resolved tenant.
        // Both filters are combined via a single lambda per entity to avoid EF Core
        // "only one query filter per entity type" limitation.
        foreach (
            var entityType in modelBuilder
                .Model.GetEntityTypes()
                .Where(et => typeof(Entity).IsAssignableFrom(et.ClrType))
        )
        {
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
            var methodName = isTenantEntity
                ? nameof(CreateCombinedFilter)
                : nameof(CreateSoftDeleteFilter);
            var method = typeof(ApplicationDbContext)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);
            var lambda = (LambdaExpression)method.Invoke(this, null)!;
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>Soft-delete only — for non-tenant-scoped entities (Plan, AuditLog).</summary>
    private static LambdaExpression CreateSoftDeleteFilter<TEntity>()
        where TEntity : Entity => (Expression<Func<TEntity, bool>>)(e => !e.IsDeleted);

    /// <summary>Soft-delete + tenant isolation — for all ITenantEntity types.</summary>
    private LambdaExpression CreateCombinedFilter<TEntity>()
        where TEntity : Entity, ITenantEntity =>
        (Expression<Func<TEntity, bool>>)(
            e => !e.IsDeleted && (!tenantContext.IsResolved || e.TenantId == tenantContext.TenantId)
        );

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-stamp TenantId on new ITenantEntity entries — never trust client payloads
        if (tenantContext.IsResolved)
        {
            foreach (
                var entry in ChangeTracker
                    .Entries<ITenantEntity>()
                    .Where(e => e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty)
            )
            {
                entry.Entity.TenantId = tenantContext.TenantId;
            }
        }

        // Auto-stamp UpdatedAt via EF property accessor (respects protected set)
        var now = DateTime.UtcNow;
        foreach (
            var entry in ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Modified)
        )
            entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
