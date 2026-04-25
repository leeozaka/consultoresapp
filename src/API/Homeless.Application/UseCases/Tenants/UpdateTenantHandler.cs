using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Tenants;

public sealed class UpdateTenantHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(
        UpdateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        // Check slug uniqueness if it changed
        if (!string.Equals(tenant.Slug, request.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var slugExists = await tenantReadRepository
                .ExistsBySlugAsync(request.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (slugExists)
                return Result.Conflict($"Slug '{request.Slug}' is already taken.");
        }

        tenant.UpdateDetails(request.Name, request.Slug, request.ContactEmail, request.ContactPhone);

        // Handle custom domain
        if (!string.IsNullOrWhiteSpace(request.CustomDomain))
        {
            if (!tenant.HasEntitlement("custom_domain"))
                tenant.GrantEntitlement("custom_domain");

            tenant.SetCustomDomain(request.CustomDomain);
        }
        else
        {
            tenant.SetCustomDomain(null);
        }

        if (request.NextBillingDate.HasValue)
            tenant.SetNextBillingDate(DateTime.SpecifyKind(request.NextBillingDate.Value, DateTimeKind.Utc));

        await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(tenant.ToResponse());
    }
}
