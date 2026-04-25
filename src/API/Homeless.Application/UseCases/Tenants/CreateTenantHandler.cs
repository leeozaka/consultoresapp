using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class CreateTenantHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var slugExists = await tenantReadRepository
            .ExistsBySlugAsync(request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            return Result.Conflict($"Slug '{request.Slug}' is already taken.");

        var tenant = Tenant.Create(request.Name, request.Slug, request.ContactEmail, request.ContactPhone);

        if (!string.IsNullOrWhiteSpace(request.CustomDomain))
        {
            tenant.GrantEntitlement("custom_domain");
            tenant.SetCustomDomain(request.CustomDomain);
        }

        if (request.NextBillingDate.HasValue)
            tenant.SetNextBillingDate(DateTime.SpecifyKind(request.NextBillingDate.Value, DateTimeKind.Utc));

        await tenantWriteRepository.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Created(tenant.ToResponse());
    }
}
