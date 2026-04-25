using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

/// <summary>
/// Returns publicly-visible tenant data (including branding) for a given slug.
/// Used by the portal frontend to initialize tenant context before authentication.
/// </summary>
public sealed record GetTenantBySlugQuery(string Slug) : IRequest<Result<TenantResponse>>;
