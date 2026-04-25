using Microsoft.EntityFrameworkCore;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence.Repositories.Read;

/// <summary>
/// Query service for <see cref="PlanResponse"/>.
/// Projects directly to DTO at the SQL level.
/// </summary>
public sealed class PlanQueryService(ReadDbContext context) : IPlanQueryService
{
    public async Task<IReadOnlyList<PlanResponse>> GetActivePlansAsync(CancellationToken cancellationToken = default)
    {
        return await Project(context.Plans.Where(p => p.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PlanResponse>> GetAllPlansAsync(CancellationToken cancellationToken = default)
    {
        return await Project(context.Plans)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PlanResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Project(context.Plans.Where(p => p.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<PlanResponse> Project(IQueryable<global::Homeless.Domain.Entities.Plan> query)
        => query.Select(p => new PlanResponse(
            p.Id,
            p.Name,
            p.Description,
            p.Price.AmountInCents / 100m,
            p.Price.Currency.Code,
            p.MaxProperties,
            p.VideoUpload,
            p.AiDescriptions,
            p.CustomDomain,
            p.PremiumAnalytics,
            p.PortalTheme,
            p.StripePriceId,
            p.IsActive));
}
