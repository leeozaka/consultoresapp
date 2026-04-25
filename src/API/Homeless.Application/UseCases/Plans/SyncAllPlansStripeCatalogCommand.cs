using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Plans;

[RequireRole(Roles.SuperAdmin)]
public sealed record SyncAllPlansStripeCatalogCommand : IRequest<Result<IReadOnlyList<PlanResponse>>>;
