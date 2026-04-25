using System.Linq.Expressions;
using System.Reflection;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homeless.Infrastructure.Persistence.Context;

public sealed class ReadDbContext(
    DbContextOptions<ReadDbContext> options,
    ITenantContext tenantContext
) : DbContext(options)
{
    public IQueryable<Tenant> Tenants => Set<Tenant>().AsNoTracking();
    public IQueryable<Property> Properties => Set<Property>().AsNoTracking();
    public IQueryable<Plan> Plans => Set<Plan>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Mirror the same global query filters as ApplicationDbContext:
        //   1. Soft-delete: excludes rows where IsDeleted = true.
        //   2. Tenant isolation (ITenantEntity only): restricts to the resolved tenant.
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
            const BindingFlags filterMethodFlags =
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
            var methodInfo = typeof(ReadDbContext).GetMethod(methodName, filterMethodFlags)!;
            var genericMethod = methodInfo.MakeGenericMethod(entityType.ClrType);
            var lambda = (LambdaExpression)(
                methodInfo.IsStatic
                    ? genericMethod.Invoke(null, null)!
                    : genericMethod.Invoke(this, null)!
            );
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>Soft-delete only — for non-tenant-scoped entities (Plan).</summary>
    private static LambdaExpression CreateSoftDeleteFilter<TEntity>()
        where TEntity : Entity => (Expression<Func<TEntity, bool>>)(e => !e.IsDeleted);

    /// <summary>Soft-delete + tenant isolation — for all ITenantEntity types.</summary>
    private LambdaExpression CreateCombinedFilter<TEntity>()
        where TEntity : Entity, ITenantEntity =>
        (Expression<Func<TEntity, bool>>)(
            e => !e.IsDeleted && (!tenantContext.IsResolved || e.TenantId == tenantContext.TenantId)
        );
}
