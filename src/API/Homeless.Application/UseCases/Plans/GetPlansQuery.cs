using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Plans;

/// <param name="ActiveOnly">When true, returns only plans visible to tenants. SuperAdmin sets this to false to see all.</param>
public sealed record GetPlansQuery(bool ActiveOnly = true) : IRequest<Result<IReadOnlyList<PlanResponse>>>;
