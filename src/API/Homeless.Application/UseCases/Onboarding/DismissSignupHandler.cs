using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using MediatR;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Onboarding;

public sealed class DismissSignupHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IIdentityService identityService,
    IOnboardingStatusStream onboardingStatusStream,
    IKeyValueStore keyValueStore,
    IUnitOfWork unitOfWork) : IRequestHandler<DismissSignupCommand, Result>
{
    public async Task<Result> Handle(
        DismissSignupCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantReadRepository
            .GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant '{request.TenantId}' not found.");

        if (tenant.Status != TenantStatus.Pending || tenant.PaymentStatus != "none")
            return Result.Conflict("Only pending signups with no payment can be dismissed.");

        var storedToken = await keyValueStore
            .GetAsync<string>(SignupHandler.DismissTokenKey(request.TenantId), cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
            return Result.Unauthorized();

        var requestBytes = Encoding.UTF8.GetBytes(request.DismissToken);
        var storedBytes = Encoding.UTF8.GetBytes(storedToken);

        if (!CryptographicOperations.FixedTimeEquals(requestBytes, storedBytes))
            return Result.Unauthorized();

        await onboardingStatusStream.PublishAsync(new OnboardingStatusEvent(
            TenantId: request.TenantId,
            Status: "abandoned",
            Message: "Cadastro cancelado pelo usuário."),
            cancellationToken).ConfigureAwait(false);

        if (tenant.OwnerUserId.HasValue)
        {
            await identityService
                .DeleteUserAsync(tenant.OwnerUserId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        await tenantWriteRepository
            .DeleteAsync(tenant.Id, cancellationToken)
            .ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await keyValueStore
            .RemoveAsync(SignupHandler.DismissTokenKey(request.TenantId), cancellationToken)
            .ConfigureAwait(false);

        return Result.NoContent();
    }
}
