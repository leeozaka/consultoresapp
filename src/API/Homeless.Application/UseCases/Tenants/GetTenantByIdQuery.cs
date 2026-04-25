using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

/// <summary>
/// Returns tenant data for a given tenant ID.
/// Used by the dashboard's "Meu Site" self-service endpoint.
/// </summary>
public sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<Result<TenantResponse>>;
