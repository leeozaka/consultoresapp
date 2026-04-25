using FluentAssertions;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Homeless.Infrastructure.Resilience;
using Homeless.Infrastructure.Sagas.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Homeless.UnitTests.Sagas;

public sealed class PaymentStateMachineTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private IPaymentGateway _paymentGateway = null!;
    private IKeyValueStore _keyValueStore = null!;
    private ITenantReadRepository _tenantReadRepository = null!;
    private ITenantWriteRepository _tenantWriteRepository = null!;
    private IPlanReadRepository _planReadRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IPaymentEventStream _paymentEventStream = null!;
    private IOnboardingStatusStream _onboardingStatusStream = null!;
    private IIdentityService _identityService = null!;

    public async Task InitializeAsync()
    {
        _paymentGateway = Substitute.For<IPaymentGateway>();
        _keyValueStore = Substitute.For<IKeyValueStore>();
        _tenantReadRepository = Substitute.For<ITenantReadRepository>();
        _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
        _planReadRepository = Substitute.For<IPlanReadRepository>();
        _unitOfWork = new PassthroughUnitOfWork();
        _paymentEventStream = Substitute.For<IPaymentEventStream>();
        _onboardingStatusStream = Substitute.For<IOnboardingStatusStream>();
        _identityService = Substitute.For<IIdentityService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Resilience:Default:TimeoutSeconds"] = "30",
                    ["Resilience:Default:MaxRetryAttempts"] = "1",
                    ["Resilience:Default:RetryDelayMs"] = "10",
                    ["Resilience:Default:CircuitBreakerFailureRatio"] = "0.5",
                    ["Resilience:Default:CircuitBreakerSamplingDurationSeconds"] = "30",
                    ["Resilience:Default:CircuitBreakerMinimumThroughput"] = "100",
                    ["Resilience:Default:CircuitBreakerBreakDurationSeconds"] = "30",
                    ["Resilience:Database:TimeoutSeconds"] = "30",
                    ["Resilience:Database:MaxRetryAttempts"] = "1",
                    ["Resilience:Database:RetryDelayMs"] = "10",
                    ["Resilience:Database:CircuitBreakerFailureRatio"] = "0.5",
                    ["Resilience:Database:CircuitBreakerSamplingDurationSeconds"] = "30",
                    ["Resilience:Database:CircuitBreakerMinimumThroughput"] = "100",
                    ["Resilience:Database:CircuitBreakerBreakDurationSeconds"] = "30",
                }
            )
            .Build();

        _provider = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .AddScoped(_ => _paymentGateway)
            .AddScoped(_ => _keyValueStore)
            .AddScoped(_ => _tenantReadRepository)
            .AddScoped(_ => _tenantWriteRepository)
            .AddScoped(_ => _planReadRepository)
            .AddScoped(_ => _unitOfWork)
            .AddScoped(_ => _paymentEventStream)
            .AddScoped(_ => _onboardingStatusStream)
            .AddScoped(_ => _identityService)
            .AddResiliencePolicies(configuration)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<CreateCheckoutSessionConsumer>();
                cfg.AddConsumer<ActivateSubscriptionConsumer>();
                cfg.AddConsumer<PaymentSagaFailedConsumer>();

                cfg.AddSagaStateMachine<PaymentStateMachine, PaymentSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task CheckoutRequested_ShouldCreateSagaAndPublishCreateCommand()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _planReadRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new CheckoutSessionResult(
                    "cs_test_123",
                    "http://checkout/cs_test_123",
                    "pi_test_123"
                )
            );

        await _harness.Bus.Publish(
            new CheckoutSessionRequested
            {
                CorrelationId = correlationId,
                TenantId = tenantId,
                PlanId = planId,
                Amount = 9990,
                Currency = "BRL",
                SuccessUrl = "http://success",
                CancelUrl = "http://cancel",
            }
        );

        var sagaHarness = _harness.GetSagaStateMachineHarness<
            PaymentStateMachine,
            PaymentSagaState
        >();

        (await sagaHarness.Consumed.Any<CheckoutSessionRequested>()).Should().BeTrue();
        (await _harness.Published.Any<CreateCheckoutSessionCommand>()).Should().BeTrue();
    }

    [Fact]
    public async Task SuccessfulCheckout_ShouldTransitionToWaitingForPayment()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _planReadRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new CheckoutSessionResult(
                    "cs_test_456",
                    "http://checkout/cs_test_456",
                    "pi_test_456"
                )
            );

        await _harness.Bus.Publish(
            new CheckoutSessionRequested
            {
                CorrelationId = correlationId,
                TenantId = tenantId,
                PlanId = planId,
                Amount = 9990,
                Currency = "BRL",
                SuccessUrl = "http://success",
                CancelUrl = "http://cancel",
            }
        );

        (
            await _harness.Published.Any<CheckoutSessionCreated>(x =>
                x.Context.Message.CorrelationId == correlationId
            )
        )
            .Should()
            .BeTrue();

        var sagaHarness = _harness.GetSagaStateMachineHarness<
            PaymentStateMachine,
            PaymentSagaState
        >();
        var saga = sagaHarness.Sagas.ContainsInState(
            correlationId,
            sagaHarness.StateMachine,
            sagaHarness.StateMachine.WaitingForPayment
        );
        saga.Should().NotBeNull();
    }

    [Fact]
    public async Task PaymentExpired_ShouldTransitionToFaulted()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _planReadRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new CheckoutSessionResult(
                    "cs_test_789",
                    "http://checkout/cs_test_789",
                    "pi_test_789"
                )
            );

        await _harness.Bus.Publish(
            new CheckoutSessionRequested
            {
                CorrelationId = correlationId,
                TenantId = tenantId,
                PlanId = planId,
                Amount = 9990,
                Currency = "BRL",
                SuccessUrl = "http://success",
                CancelUrl = "http://cancel",
            }
        );

        (
            await _harness.Published.Any<CheckoutSessionCreated>(x =>
                x.Context.Message.CorrelationId == correlationId
            )
        )
            .Should()
            .BeTrue();

        await _harness.Bus.Publish(
            new PaymentWebhookReceived
            {
                CorrelationId = correlationId,
                EventId = "evt_test_expired",
                EventType = "checkout.session.expired",
                SessionId = "cs_test_789",
                Status = "expired",
            }
        );

        (
            await _harness.Published.Any<PaymentSagaFailed>(x =>
                x.Context.Message.CorrelationId == correlationId
            )
        )
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task GatewayFailure_ShouldPublishCheckoutFailed()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _planReadRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);

        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo =>
                Task.FromException<CheckoutSessionResult>(
                    new HttpRequestException("Gateway unreachable")
                )
            );

        await _harness.Bus.Publish(
            new CheckoutSessionRequested
            {
                CorrelationId = correlationId,
                TenantId = tenantId,
                PlanId = planId,
                Amount = 9990,
                Currency = "BRL",
                SuccessUrl = "http://success",
                CancelUrl = "http://cancel",
            }
        );

        (
            await _harness.Published.Any<CheckoutSessionFailed>(x =>
                x.Context.Message.CorrelationId == correlationId
            )
        )
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ActivationMissingTenant_ShouldPublishSagaFailed()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository
            .GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(tenant, (Tenant?)null);
        _planReadRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns(plan);
        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new CheckoutSessionResult("cs_test_missing_tenant", "http://checkout", ""));

        await _harness.Bus.Publish(new CheckoutSessionRequested
        {
            CorrelationId = correlationId,
            TenantId = tenantId,
            PlanId = planId,
            Amount = 9990,
            Currency = "BRL",
            SuccessUrl = "http://success",
            CancelUrl = "http://cancel",
        });

        (await _harness.Published.Any<CheckoutSessionCreated>(x =>
            x.Context.Message.CorrelationId == correlationId)).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentWebhookReceived
        {
            CorrelationId = correlationId,
            EventId = "evt_test_completed_missing_tenant",
            EventType = "checkout.session.completed",
            SessionId = "cs_test_missing_tenant",
            Status = "complete",
        });

        (await _harness.Published.Any<PaymentSagaFailed>(x =>
            x.Context.Message.CorrelationId == correlationId
            && x.Context.Message.Reason.Contains("Tenant", StringComparison.OrdinalIgnoreCase)))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ActivationMissingPlan_ShouldPublishSagaFailed()
    {
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        var plan = Plan.Create(
            "Growth",
            "Growth",
            Money.Create(9900L, Currency.BRL),
            100,
            stripePriceId: "price_growth"
        );

        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, tenantId);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(plan, planId);

        _tenantReadRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _planReadRepository
            .GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan, (Plan?)null);
        _paymentGateway
            .CreateSubscriptionCheckoutSessionAsync(
                Arg.Any<RecurringSubscriptionCheckoutRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new CheckoutSessionResult("cs_test_missing_plan", "http://checkout", ""));

        await _harness.Bus.Publish(new CheckoutSessionRequested
        {
            CorrelationId = correlationId,
            TenantId = tenantId,
            PlanId = planId,
            Amount = 9990,
            Currency = "BRL",
            SuccessUrl = "http://success",
            CancelUrl = "http://cancel",
        });

        (await _harness.Published.Any<CheckoutSessionCreated>(x =>
            x.Context.Message.CorrelationId == correlationId)).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentWebhookReceived
        {
            CorrelationId = correlationId,
            EventId = "evt_test_completed_missing_plan",
            EventType = "checkout.session.completed",
            SessionId = "cs_test_missing_plan",
            Status = "complete",
        });

        (await _harness.Published.Any<PaymentSagaFailed>(x =>
            x.Context.Message.CorrelationId == correlationId
            && x.Context.Message.Reason.Contains("Plan", StringComparison.OrdinalIgnoreCase)))
            .Should()
            .BeTrue();
    }

    private sealed class PassthroughUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default
        ) => await operation(cancellationToken);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default
        ) => await operation(cancellationToken);

        public void Dispose() { }
    }
}
