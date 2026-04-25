using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.SuperAdmin)]
public sealed record AssignTenantPlanCommand(
    Guid TenantId,
    Guid PlanId
) : IRequest<Result<TenantResponse>>;
