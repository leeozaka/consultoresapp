using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Payments;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class GetTenantBillingOverviewHandlerTests
{
    private readonly ITenantQueryService _tenantQueryService = Substitute.For<ITenantQueryService>();
    private readonly IPlanQueryService _planQueryService = Substitute.For<IPlanQueryService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();

    private GetTenantBillingOverviewHandler CreateHandler() =>
        new(_tenantQueryService, _planQueryService, _tenantContext, _paymentGateway);

    private static TenantResponse BuildTenantResponse(
        Guid id,
        Guid? planId = null,
        string? stripeCustomerId = null,
        string? stripeSubscriptionId = null,
        string paymentStatus = "none",
        DateTime? lastPaymentDate = null,
        DateTime? nextBillingDate = null)
        => new(
            Id: id,
            Name: "Acme",
            Type: TenantType.Agency,
            Slug: "acme",
            CustomDomain: null,
            FrontendOrigin: null,
            Status: TenantStatus.Active,
            PlanId: planId,
            OwnerUserId: null,
            PortalLayoutMode: "Default",
            PortalTheme: "default",
            ContactEmail: "billing@acme.dev",
            ContactPhone: null,
            Branding: new BrandingConfigResponse(null, "#1A73E8", "#F5A623", "Acme", null, null, null),
            Entitlements: [],
            StripeCustomerId: stripeCustomerId,
            StripeSubscriptionId: stripeSubscriptionId,
            PaymentStatus: paymentStatus,
            LastPaymentDate: lastPaymentDate,
            NextBillingDate: nextBillingDate,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

    private static PlanResponse BuildPlanResponse(Guid id, string name = "Growth") =>
        new(
            Id: id,
            Name: name,
            Description: $"{name} plan",
            PricePerMonth: 199m,
            CurrencyCode: "BRL",
            MaxProperties: 100,
            VideoUpload: false,
            AiDescriptions: false,
            CustomDomain: false,
            PremiumAnalytics: true,
            PortalTheme: "default",
            StripePriceId: "price_growth",
            IsActive: true);

    [Fact]
    public async Task Handle_ShouldReturnBillingOverviewWithRecentTransactions()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var lastPayment = DateTime.UtcNow.AddDays(-2);
        var nextBilling = DateTime.UtcNow.AddDays(28);

        var tenant = BuildTenantResponse(
            tenantId,
            planId: planId,
            stripeCustomerId: "cus_123",
            stripeSubscriptionId: "sub_123",
            paymentStatus: "paid",
            lastPaymentDate: lastPayment,
            nextBillingDate: nextBilling);

        var plan = BuildPlanResponse(planId);

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenantId);

        _tenantQueryService
            .GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _planQueryService
            .GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);

        _paymentGateway
            .GetBillingOverviewAsync(
                Arg.Is<BillingOverviewRequest>(x =>
                    x.TenantId == tenantId &&
                    x.StripeCustomerId == "cus_123" &&
                    x.StripeSubscriptionId == "sub_123" &&
                    x.RecentTransactionsLimit == 5),
                Arg.Any<CancellationToken>())
            .Returns(new BillingOverviewResult(
                SubscriptionStatus: "active",
                HasRecurringPayment: true,
                NextPaymentDate: nextBilling,
                GracePeriodEndsAt: null,
                AvailabilityEndsAt: nextBilling,
                RecentTransactions:
                [
                    new BillingTransactionResult(
                        Id: "in_123",
                        Type: "invoice",
                        Status: "paid",
                        Amount: 19900,
                        Currency: "brl",
                        OccurredAt: lastPayment,
                        Description: "Growth monthly subscription",
                        HostedInvoiceUrl: "https://stripe.test/invoices/in_123")
                ]));

        var result = await CreateHandler().Handle(new GetTenantBillingOverviewQuery(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SubscriptionStatus.Should().Be("active");
        result.Value.PlanId.Should().Be(planId);
        result.Value.PlanName.Should().Be("Growth");
        result.Value.HasRecurringPayment.Should().BeTrue();
        result.Value.CanManageBilling.Should().BeTrue();
        result.Value.RecentTransactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenantId);
        _tenantQueryService.GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantResponse?)null);

        var result = await CreateHandler().Handle(new GetTenantBillingOverviewQuery(), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
