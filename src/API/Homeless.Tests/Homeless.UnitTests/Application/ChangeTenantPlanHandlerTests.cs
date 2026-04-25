using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Sagas.Payment;
using Homeless.Application.UseCases.Plans;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class ChangeTenantPlanHandlerTests
{
    private readonly IPlanReadRepository _planReadRepository = Substitute.For<IPlanReadRepository>();
    private readonly ITenantReadRepository _tenantReadRepository = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
    private readonly IPaymentSagaService _paymentSagaService = Substitute.For<IPaymentSagaService>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangeTenantPlanHandler CreateHandler() =>
        new(
            _planReadRepository,
            _tenantReadRepository,
            _tenantWriteRepository,
            _paymentSagaService,
            _paymentGateway,
            _cacheService,
            _tenantContext,
            _unitOfWork);

    [Fact]
    public async Task Handle_WhenTenantHasRecurringSubscription_ShouldUpdateStripeSubscriptionAndPersistPlan()
    {
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        tenant.Activate();

        var currentPlan = Plan.Create(
            name: "Starter",
            description: "Starter plan",
            price: Money.Create(9900L, Currency.BRL),
            maxProperties: 25,
            stripePriceId: "price_starter");

        tenant.ChangePlan(currentPlan);
        tenant.SetStripeCustomerId("cus_123");
        tenant.SetStripeSubscriptionId("sub_123");

        var targetPlan = Plan.Create(
            name: "Growth",
            description: "Growth plan",
            price: Money.Create(19900L, Currency.BRL),
            maxProperties: 100,
            premiumAnalytics: true,
            stripePriceId: "price_growth");

        _tenantContext.TenantId.Returns(tenant.Id);
        _planReadRepository.GetByIdAsync(targetPlan.Id, Arg.Any<CancellationToken>()).Returns(targetPlan);
        _planReadRepository.GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>()).Returns(currentPlan);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        _paymentGateway.UpdateSubscriptionAsync(
                "sub_123",
                "price_growth",
                true,
                Arg.Any<CancellationToken>())
            .Returns(new BillingSubscriptionUpdateResult(
                SubscriptionId: "sub_123",
                SubscriptionStatus: "active",
                CurrentPeriodEnd: DateTime.UtcNow.AddDays(30)));

        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeTenantPlanCommand(targetPlan.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Plan.Id.Should().Be(targetPlan.Id);
        result.Value.RequiresCheckout.Should().BeFalse();
        tenant.PlanId.Should().Be(targetPlan.Id);

        await _paymentGateway.Received(1).UpdateSubscriptionAsync(
            "sub_123",
            "price_growth",
            true,
            Arg.Any<CancellationToken>());

        await _tenantWriteRepository.Received(1).UpdateAsync(tenant, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _paymentSagaService.DidNotReceive().InitiateCheckoutAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTenantNeedsToRetryPaymentForCurrentPlan_ShouldCreateCheckoutSession()
    {
        var tenant = Tenant.Create("Acme", "acme", "billing@acme.dev");
        tenant.Activate();

        var currentPlan = Plan.Create(
            name: "Starter",
            description: "Starter plan",
            price: Money.Create(9900L, Currency.BRL),
            maxProperties: 25,
            stripePriceId: "price_starter");

        tenant.ChangePlan(currentPlan);

        _tenantContext.TenantId.Returns(tenant.Id);
        _planReadRepository.GetByIdAsync(currentPlan.Id, Arg.Any<CancellationToken>()).Returns(currentPlan);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        _paymentSagaService.InitiateCheckoutAsync(
                tenant.Id,
                currentPlan.Id,
                9900,
                currentPlan.Price.Currency.Code,
                "http://consultor.localhost/dashboard/plan?checkout=success",
                "http://consultor.localhost/dashboard/plan?checkout=cancel",
                Arg.Any<CancellationToken>())
            .Returns(new PaymentSagaResult(
                Success: true,
                SessionUrl: "https://checkout.stripe.test/session_123",
                StripeSessionId: "cs_test_123",
                ErrorMessage: null));

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangeTenantPlanCommand(
                currentPlan.Id,
                SuccessUrl: "http://consultor.localhost/dashboard/plan?checkout=success",
                CancelUrl: "http://consultor.localhost/dashboard/plan?checkout=cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresCheckout.Should().BeTrue();
        result.Value.CheckoutUrl.Should().Be("https://checkout.stripe.test/session_123");
        result.Value.UpdatedInPlace.Should().BeFalse();

        await _paymentSagaService.Received(1).InitiateCheckoutAsync(
            tenant.Id,
            currentPlan.Id,
            9900,
            currentPlan.Price.Currency.Code,
            "http://consultor.localhost/dashboard/plan?checkout=success",
            "http://consultor.localhost/dashboard/plan?checkout=cancel",
            Arg.Any<CancellationToken>());

        await _tenantWriteRepository.DidNotReceive().UpdateAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
