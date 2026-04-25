using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Infrastructure.Sagas.Consumers;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class PaymentSagaFailedConsumerTests
{
    private readonly ITenantReadRepository _tenantRead = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWrite = Substitute.For<ITenantWriteRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IOnboardingStatusStream _onboardingStream = Substitute.For<IOnboardingStatusStream>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private PaymentSagaFailedConsumer CreateConsumer() => new(
        _tenantRead,
        _tenantWrite,
        _identityService,
        _onboardingStream,
        _unitOfWork,
        NullLogger<PaymentSagaFailedConsumer>.Instance);

    private static ConsumeContext<PaymentSagaFailed> BuildContext(Guid tenantId, Guid correlationId = default)
    {
        var ctx = Substitute.For<ConsumeContext<PaymentSagaFailed>>();
        ctx.Message.Returns(new PaymentSagaFailed
        {
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            TenantId = tenantId,
            Reason = "Payment session expired"
        });
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    [Fact]
    public async Task Consume_WhenTenantIsPendingWithNoPayment_ShouldPublishSseDeleteUserAndDeleteTenant()
    {
        var userId = Guid.NewGuid();
        var tenant = Tenant.Create("Minha Imob", "minha-imob", "contato@minha-imob.com.br");
        tenant.SetOwnerUserId(userId);
        var context = BuildContext(tenant.Id);

        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _identityService.DeleteUserAsync(userId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateConsumer().Consume(context);

        await _onboardingStream.Received(1).PublishAsync(
            Arg.Is<OnboardingStatusEvent>(e =>
                e.TenantId == tenant.Id &&
                e.Status == "abandoned"),
            Arg.Any<CancellationToken>());

        await _identityService.Received(1).DeleteUserAsync(userId, Arg.Any<CancellationToken>());
        await _tenantWrite.Received(1).DeleteAsync(tenant.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenTenantNotFound_ShouldNoOp()
    {
        var tenantId = Guid.NewGuid();
        _tenantRead.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        await CreateConsumer().Consume(BuildContext(tenantId));

        await _onboardingStream.DidNotReceive().PublishAsync(Arg.Any<OnboardingStatusEvent>(), Arg.Any<CancellationToken>());
        await _identityService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _tenantWrite.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenTenantIsActive_ShouldSkipCleanup()
    {
        var tenant = Tenant.Create("Imob Ativa", "imob-ativa", "contato@imob-ativa.com.br");
        tenant.Activate();
        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateConsumer().Consume(BuildContext(tenant.Id));

        await _tenantWrite.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _identityService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenTenantIsPendingButPaymentStatusIsPaid_ShouldSkipCleanup()
    {
        // Pending tenant that already has a payment (e.g. awaiting SuperAdmin approval)
        var tenant = Tenant.Create("Imob Paga", "imob-paga", "contato@imob-paga.com.br");
        tenant.RecordPaymentSucceeded(DateTime.UtcNow, DateTime.UtcNow.AddMonths(1)); // sets PaymentStatus = "paid"
        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateConsumer().Consume(BuildContext(tenant.Id));

        await _tenantWrite.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _identityService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenTenantHasNoOwnerUserId_ShouldDeleteTenantWithoutDeletingUser()
    {
        var tenant = Tenant.Create("Imob Sem Owner", "imob-sem-owner", "contato@imob-sem.com.br");
        // OwnerUserId is null — no user was created before the failure
        _tenantRead.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateConsumer().Consume(BuildContext(tenant.Id));

        await _identityService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _tenantWrite.Received(1).DeleteAsync(tenant.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
