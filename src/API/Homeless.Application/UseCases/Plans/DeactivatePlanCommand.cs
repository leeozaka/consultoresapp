using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;

namespace Homeless.Application.UseCases.Plans;

[RequireRole(Roles.SuperAdmin)]
public sealed record DeactivatePlanCommand(Guid PlanId) : IRequest<Result>;
