using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class GetOnboardingStatusHandlerTests
{
    private readonly ITenantQueryService _tenantQueryService = Substitute.For<ITenantQueryService>();

    private GetOnboardingStatusHandler CreateHandler() =>
        new(_tenantQueryService);

    private static TenantResponse BuildTenantResponse(
        Guid? id = null,
        string name = "Imob",
        string slug = "imob",
        TenantStatus status = TenantStatus.Pending,
        string paymentStatus = "none",
        Guid? planId = null)
        => new(
            Id: id ?? Guid.NewGuid(),
            Name: name,
            Type: TenantType.Agency,
            Slug: slug,
            CustomDomain: null,
            FrontendOrigin: null,
            Status: status,
            PlanId: planId,
            OwnerUserId: null,
            PortalLayoutMode: "Default",
            PortalTheme: "default",
            ContactEmail: "contato@imob.com.br",
            ContactPhone: null,
            Branding: new BrandingConfigResponse(null, "#1A73E8", "#F5A623", name, null, null, null),
            Entitlements: [],
            StripeCustomerId: null,
            StripeSubscriptionId: null,
            PaymentStatus: paymentStatus,
            LastPaymentDate: null,
            NextBillingDate: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenantQueryService
            .GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantResponse?)null);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(tenantId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantPendingNoPayment_ShouldReturnPendingPaymentState()
    {
        var tenant = BuildTenantResponse(status: TenantStatus.Pending, paymentStatus: "none");

        _tenantQueryService
            .GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(tenant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OnboardingState.Should().Be("pending_payment");
        result.Value.TenantStatus.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_WhenTenantPendingWithPaidStatus_ShouldReturnAwaitingApprovalState()
    {
        var tenant = BuildTenantResponse(status: TenantStatus.Pending, paymentStatus: "paid");

        _tenantQueryService
            .GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(tenant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OnboardingState.Should().Be("awaiting_approval");
        result.Value.PaymentStatus.Should().Be("paid");
    }

    [Fact]
    public async Task Handle_WhenTenantActive_ShouldReturnActiveState()
    {
        var tenant = BuildTenantResponse(slug: "imob", status: TenantStatus.Active, paymentStatus: "paid");

        _tenantQueryService
            .GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var result = await CreateHandler().Handle(
            new GetOnboardingStatusQuery(tenant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OnboardingState.Should().Be("active");
        result.Value.TenantStatus.Should().Be("active");
        result.Value.Slug.Should().Be("imob");
    }
}
