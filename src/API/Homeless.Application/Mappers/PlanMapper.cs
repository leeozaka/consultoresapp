using Homeless.Application.DTOs;
using Homeless.Domain.Entities;

namespace Homeless.Application.Mappers;

public static class PlanMapper
{
    public static PlanResponse ToResponse(this Plan plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price.AmountInCents / 100m,
            plan.Price.Currency.Code,
            plan.MaxProperties,
            plan.VideoUpload,
            plan.AiDescriptions,
            plan.CustomDomain,
            plan.PremiumAnalytics,
            plan.PortalTheme,
            plan.StripePriceId,
            plan.IsActive);
}
