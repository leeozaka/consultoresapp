using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Plans;

[RequireRole(Roles.SuperAdmin)]
public sealed record CreatePlanCommand(
    string Name,
    string Description,
    decimal PricePerMonth,
    int MaxProperties,
    bool VideoUpload,
    bool AiDescriptions,
    bool CustomDomain,
    bool PremiumAnalytics,
    string PortalTheme,
    string? StripePriceId
) : IRequest<Result<PlanResponse>>;
