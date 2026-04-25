using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Application.UseCases.Onboarding;

[SuppressMessage(
    "Maintainability",
    "S107:Methods should not have too many parameters",
    Justification = "Signup orchestrates slug/email checks, tenant creation, plan assignment, identity, checkout, and key-value storage — each is a distinct dependency.")]
public sealed class SignupHandler(
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository,
    IPlanReadRepository planReadRepository,
    IIdentityService identityService,
    IPaymentSagaService paymentSagaService,
    IKeyValueStore keyValueStore,
    IUnitOfWork unitOfWork) : IRequestHandler<SignupCommand, Result<SignupResponse>>
{
    public async Task<Result<SignupResponse>> Handle(
        SignupCommand request,
        CancellationToken cancellationToken)
    {
        Tenant? tenant = null;
        Guid userId = Guid.Empty;
        bool createdNewEntities = false;

        // 1. Check slug — may be an idempotent retry of a previously failed signup
        var existingBySlug = await tenantReadRepository
            .GetBySlugAsync(request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (existingBySlug is not null)
        {
            // Allow resume only if: the tenant is still Pending and the owner email matches
            if (existingBySlug.Status != TenantStatus.Pending || existingBySlug.OwnerUserId is null)
                return Result.Conflict($"Slug '{request.Slug}' is already taken.");

            var ownerEmail = await identityService
                .GetUserEmailAsync(existingBySlug.OwnerUserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!string.Equals(ownerEmail, request.Email, StringComparison.OrdinalIgnoreCase))
                return Result.Conflict($"Slug '{request.Slug}' is already taken.");

            // Same owner retrying — reuse existing tenant and user, skip to checkout
            tenant = existingBySlug;
            userId = existingBySlug.OwnerUserId.Value;

            // Generate a fresh dismiss token for the resumed signup
            var resumeDismissToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await keyValueStore.SetAsync(
                DismissTokenKey(tenant.Id),
                resumeDismissToken,
                TimeSpan.FromHours(48),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // 2. Validate user email uniqueness
            var emailTaken = await identityService
                .EmailExistsAsync(request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (emailTaken)
                return Result.Conflict($"An account with email '{request.Email}' already exists.");

            // 3. Resolve plan
            var plan = await planReadRepository
                .GetByIdAsync(request.PlanId, cancellationToken)
                .ConfigureAwait(false);

            if (plan is null || !plan.IsActive)
                return Result.NotFound($"Plan '{request.PlanId}' not found or is no longer available.");

            // 4. Create tenant (Pending status)
            tenant = Tenant.Create(
                name: request.AgencyName,
                slug: request.Slug,
                contactEmail: request.ContactEmail,
                contactPhone: request.ContactPhone);

            tenant.ChangePlan(plan);

            await tenantWriteRepository.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // 5. Create user (inactive) and bind to tenant as TenantAdmin
            var (userCreated, newUserId, userErrors) = await identityService
                .CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, cancellationToken)
                .ConfigureAwait(false);

            if (!userCreated)
            {
                await CompensateAsync(tenant.Id, Guid.Empty, cancellationToken).ConfigureAwait(false);
                return Result.Error($"Failed to create user account: {string.Join("; ", userErrors)}");
            }

            userId = newUserId;

            var (assigned, assignErrors) = await identityService
                .AssignTenantAndRoleAsync(userId, tenant.Id, Roles.TenantAdmin, activate: false, cancellationToken)
                .ConfigureAwait(false);

            if (!assigned)
            {
                await CompensateAsync(tenant.Id, userId, cancellationToken).ConfigureAwait(false);
                return Result.Error($"Failed to assign tenant: {string.Join("; ", assignErrors)}");
            }

            // 6. Persist OwnerUserId on tenant
            tenant.SetOwnerUserId(userId);
            await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            createdNewEntities = true;
        }

        // 7. Generate dismiss token (or retrieve the one just created for resume path)
        var dismissToken = await keyValueStore
            .GetAsync<string>(DismissTokenKey(tenant.Id), cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(dismissToken))
        {
            dismissToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await keyValueStore.SetAsync(
                DismissTokenKey(tenant.Id),
                dismissToken,
                TimeSpan.FromHours(48),
                cancellationToken).ConfigureAwait(false);
        }

        // 8. Initiate Stripe checkout
        var planForCheckout = await planReadRepository
            .GetByIdAsync(tenant.PlanId!.Value, cancellationToken)
            .ConfigureAwait(false);

        if (planForCheckout is null || !planForCheckout.IsActive)
            return Result.NotFound($"Plan '{tenant.PlanId}' not found or is no longer available.");

        var successUrl = request.SuccessUrl.Replace("TENANT_ID", tenant.Id.ToString(), StringComparison.Ordinal);
        var cancelUrl = request.CancelUrl.Replace("TENANT_ID", tenant.Id.ToString(), StringComparison.Ordinal);

        var checkoutResult = await paymentSagaService.InitiateCheckoutAsync(
            tenantId: tenant.Id,
            planId: planForCheckout.Id,
            amount: planForCheckout.Price.AmountInCents,
            currency: planForCheckout.Price.Currency.Code,
            successUrl: successUrl,
            cancelUrl: cancelUrl,
            cancellationToken).ConfigureAwait(false);

        if (!checkoutResult.Success)
        {
            if (createdNewEntities)
                await CompensateAsync(tenant.Id, userId, cancellationToken).ConfigureAwait(false);

            return Result.Error($"Payment initiation failed: {checkoutResult.ErrorMessage}");
        }

        // 9. Store signup marker so the webhook handler can auto-activate after payment
        await keyValueStore.SetAsync(
            SignupCheckoutKey(checkoutResult.StripeSessionId!),
            tenant.Id.ToString(),
            cancellationToken).ConfigureAwait(false);

        return Result.Created(new SignupResponse(tenant.Id, userId, checkoutResult.SessionUrl!, dismissToken));
    }

    private async Task CompensateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        if (userId != Guid.Empty)
            await identityService.DeleteUserAsync(userId, cancellationToken).ConfigureAwait(false);

        if (tenantId != Guid.Empty)
        {
            await tenantWriteRepository.DeleteAsync(tenantId, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static string SignupCheckoutKey(string stripeSessionId) =>
        $"signup-checkout:{stripeSessionId}";

    internal static string DismissTokenKey(Guid tenantId) =>
        $"signup-dismiss:{tenantId}";
}
