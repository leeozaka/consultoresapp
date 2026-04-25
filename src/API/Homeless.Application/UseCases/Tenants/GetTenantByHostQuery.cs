using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

public sealed record GetTenantByHostQuery(string Host, string LandingDomain, string LandingTenantSlug) : IRequest<Result<TenantResponse>>;
