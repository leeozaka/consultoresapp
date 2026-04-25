using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Payments;

[RequireRole(Roles.TenantAdmin)]
public sealed record CreateBillingPortalSessionCommand(
    string ReturnUrl) : IRequest<Result<BillingPortalSessionResponse>>;
