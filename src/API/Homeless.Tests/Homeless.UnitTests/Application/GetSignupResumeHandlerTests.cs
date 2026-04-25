using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Onboarding;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class GetSignupResumeHandlerTests
{
    private readonly ITenantQueryService _tenantQueryService = Substitute.For<ITenantQueryService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    private GetSignupResumeHandler CreateHandler() => new(_tenantQueryService, _identityService);

    private static TenantResponse BuildTenantResponse(
        Guid id,
        string name = "Minha Imob",
        string slug = "minha-imob",
        TenantStatus status = TenantStatus.Pending,
        string paymentStatus = "none",
        Guid? planId = null,
        Guid? ownerUserId = null,
        string contactEmail = "contato@imob.com.br",
        string? contactPhone = "(11) 99999-9999")
        => new(
            Id: id,
            Name: name,
            Type: TenantType.Agency,
            Slug: slug,
            CustomDomain: null,
            FrontendOrigin: null,
            Status: status,
            PlanId: planId,
            OwnerUserId: ownerUserId,
            PortalLayoutMode: "Default",
            PortalTheme: "default",
            ContactEmail: contactEmail,
            ContactPhone: contactPhone,
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
    public async Task Handle_WhenPendingTenantExists_ShouldReturnResumeData()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = BuildTenantResponse(tenantId, planId: planId, ownerUserId: userId);

        _tenantQueryService.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _identityService.GetUserBasicInfoAsync(userId, Arg.Any<CancellationToken>())
            .Returns(("joao@imob.com.br", "João", "Silva"));

        var result = await CreateHandler().Handle(new GetSignupResumeQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.AgencyName.Should().Be("Minha Imob");
        result.Value.Slug.Should().Be("minha-imob");
        result.Value.ContactEmail.Should().Be("contato@imob.com.br");
        result.Value.ContactPhone.Should().Be("(11) 99999-9999");
        result.Value.PlanId.Should().Be(planId);
        result.Value.OwnerEmail.Should().Be("joao@imob.com.br");
        result.Value.OwnerFirstName.Should().Be("João");
        result.Value.OwnerLastName.Should().Be("Silva");
        result.Value.OnboardingState.Should().Be("pending_payment");
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenantQueryService.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((TenantResponse?)null);

        var result = await CreateHandler().Handle(new GetSignupResumeQuery(tenantId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantIsActive_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        var tenant = BuildTenantResponse(tenantId, status: TenantStatus.Active, paymentStatus: "paid");
        _tenantQueryService.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await CreateHandler().Handle(new GetSignupResumeQuery(tenantId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantHasPayment_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();
        var tenant = BuildTenantResponse(tenantId, status: TenantStatus.Pending, paymentStatus: "paid");
        _tenantQueryService.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await CreateHandler().Handle(new GetSignupResumeQuery(tenantId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
