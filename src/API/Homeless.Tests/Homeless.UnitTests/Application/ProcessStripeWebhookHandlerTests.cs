using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Application.UseCases.Webhooks;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class ProcessStripeWebhookHandlerTests
{
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IKeyValueStore _keyValueStore = Substitute.For<IKeyValueStore>();
    private readonly IPaymentEventStream _paymentEventStream = Substitute.For<IPaymentEventStream>();
    private readonly IOnboardingStatusStream _onboardingStatusStream = Substitute.For<IOnboardingStatusStream>();
    private readonly ITenantReadRepository _tenantReadRepository = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ISiteBuildChannel _siteBuildChannel = Substitute.For<ISiteBuildChannel>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBus _bus = Substitute.For<IBus>();
    private readonly ILogger<ProcessStripeWebhookHandler> _logger =
        Substitute.For<ILogger<ProcessStripeWebhookHandler>>();

    private ProcessStripeWebhookHandler CreateHandler() =>
        new(
            _paymentGateway,
            _cacheService,
            _keyValueStore,
            _paymentEventStream,
            _onboardingStatusStream,
            _tenantReadRepository,
            _tenantWriteRepository,
            _identityService,
            _siteBuildChannel,
            _eventBus,
            _unitOfWork,
            _bus,
            _logger);

    [Fact]
    public async Task Handle_WhenSubscriptionUpdated_ShouldSyncBillingAndPersist()
    {
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        tenant.Activate();
        tenant.SetStripeCustomerId("cus_123");
        tenant.SetStripeSubscriptionId("sub_123");

        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var webhookEvent = new PaymentWebhookEvent(
            EventId: "evt_sub_123",
            EventType: "customer.subscription.updated",
            SessionId: "sub_123",
            PaymentIntentId: string.Empty,
            SubscriptionId: "sub_123",
            Status: "active",
            AmountTotal: 0,
            Currency: "brl",
            Metadata: new Dictionary<string, string>
            {
                ["tenant_id"] = tenant.Id.ToString()
            },
            CustomerId: "cus_123",
            SubscriptionStatus: "active");

        var result = await CreateHandler().Handle(
            new ProcessStripeWebhookCommand(webhookEvent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tenantWriteRepository.Received().UpdateAsync(tenant, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSubscriptionDeleted_ShouldMarkCanceledAndPersist()
    {
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        tenant.Activate();
        tenant.SetStripeSubscriptionId("sub_456");

        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var webhookEvent = new PaymentWebhookEvent(
            EventId: "evt_del_456",
            EventType: "customer.subscription.deleted",
            SessionId: string.Empty,
            PaymentIntentId: string.Empty,
            SubscriptionId: "sub_456",
            Status: "canceled",
            AmountTotal: 0,
            Currency: "brl",
            Metadata: new Dictionary<string, string>
            {
                ["tenant_id"] = tenant.Id.ToString()
            });

        var result = await CreateHandler().Handle(
            new ProcessStripeWebhookCommand(webhookEvent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tenantWriteRepository.Received().UpdateAsync(tenant, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCheckoutCompletedHasCorrelationMetadata_ShouldPublishWebhookToSagaWithoutKeyValueLookup()
    {
        var correlationId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var webhookEvent = new PaymentWebhookEvent(
            EventId: "evt_checkout_corr",
            EventType: "checkout.session.completed",
            SessionId: "cs_test_corr",
            PaymentIntentId: "pi_test_corr",
            SubscriptionId: "sub_test_corr",
            Status: "complete",
            AmountTotal: 9700,
            Currency: "brl",
            Metadata: new Dictionary<string, string>
            {
                ["tenant_id"] = tenant.Id.ToString(),
                ["correlation_id"] = correlationId.ToString()
            },
            CustomerId: "cus_test_corr");

        var result = await CreateHandler().Handle(
            new ProcessStripeWebhookCommand(webhookEvent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _bus.Received(1).Publish(
            Arg.Is<PaymentWebhookReceived>(message =>
                message.CorrelationId == correlationId
                && message.SessionId == "cs_test_corr"
                && message.Status == "complete"),
            Arg.Any<CancellationToken>());
        await _keyValueStore.DidNotReceive().GetAsync<string>(
            "stripe-session:cs_test_corr",
            Arg.Any<CancellationToken>());
    }
}
