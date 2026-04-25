using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Payments;

public sealed record GetTenantBillingOverviewQuery(
    int RecentTransactionsLimit = 10) : IRequest<Result<TenantBillingOverviewResponse>>;
