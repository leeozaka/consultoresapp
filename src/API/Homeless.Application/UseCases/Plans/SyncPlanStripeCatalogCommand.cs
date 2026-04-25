using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Plans;

[RequireRole(Roles.SuperAdmin)]
public sealed record SyncPlanStripeCatalogCommand(Guid PlanId) : IRequest<Result<PlanResponse>>;
