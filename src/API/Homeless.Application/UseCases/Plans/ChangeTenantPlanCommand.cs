using Ardalis.Result;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using MediatR;

namespace Homeless.Application.UseCases.Plans;

[RequireRole(Roles.TenantAdmin)]
public sealed record ChangeTenantPlanCommand(
    Guid PlanId,
    string? SuccessUrl = null,
    string? CancelUrl = null
) : IRequest<Result<ChangePlanResponse>>;
