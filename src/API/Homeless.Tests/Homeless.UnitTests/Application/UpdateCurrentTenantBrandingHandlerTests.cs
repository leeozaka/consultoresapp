using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Tenants;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class UpdateCurrentTenantBrandingHandlerTests
{
    private readonly ITenantReadRepository _tenantReadRepository = Substitute.For<ITenantReadRepository>();
    private readonly ITenantWriteRepository _tenantWriteRepository = Substitute.For<ITenantWriteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private UpdateCurrentTenantBrandingHandler CreateHandler() =>
        new(_tenantReadRepository, _tenantWriteRepository, _unitOfWork, _tenantContext);

    [Fact]
    public async Task Handle_ShouldReject_WhenCustomFrontend()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.SetFrontendOrigin("https://custom.example");

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_ShouldRejectRichContent_WhenPortalThemeDefault()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "default" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        "https://x.test/h.jpg",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null))),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldClearPortalContent_WhenThemeDefault()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "default" });
        tenant.UpdateBranding(tenant.Branding with
        {
            PortalContent = new PortalBrandingContent { HeroHeadline = "Old" },
        });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(null, "#123456", "#abcdef", "A", null, null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Branding.PortalContent.Should().BeNull();
        await _tenantWriteRepository.Received(1).UpdateAsync(tenant, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRejectFeatureCards_WhenMinimalTheme()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "minimal" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        null, null, null, null, null, null, null,
                        FeatureCard1Title: "Card 1"))),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldRejectSecondaryCta_WhenMinimalTheme()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "minimal" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        null, null, null, null, null, null, null,
                        HeroSecondaryCta: "Agendar visita"))),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldAcceptAllNewFields_WhenPremiumTheme()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "premium" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        "https://x.test/h.jpg",
                        "Headline",
                        "Sub",
                        "CTA",
                        "https://x.test",
                        "Intro",
                        "https://x.test/s.jpg",
                        Stat1Label: "Imóveis",
                        Stat1Value: "500+",
                        Stat2Label: "Clientes",
                        Stat2Value: "200+",
                        FeatureCard1Title: "Card 1",
                        FeatureCard1Description: "Desc 1",
                        FeatureCard1Icon: "home",
                        FeatureCard2Title: "Card 2",
                        FeatureCard2Description: "Desc 2",
                        FeatureCard2Icon: "search",
                        FeatureCard3Title: "Card 3",
                        FeatureCard3Description: "Desc 3",
                        FeatureCard3Icon: "star",
                        WhatsappNumber: "+5511999999999",
                        HeroSecondaryCta: "Agendar consulta"))),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Branding.PortalContent.Should().NotBeNull();
        tenant.Branding.PortalContent!.Stat1Label.Should().Be("Imóveis");
        tenant.Branding.PortalContent!.FeatureCard1Title.Should().Be("Card 1");
        tenant.Branding.PortalContent!.WhatsappNumber.Should().Be("+5511999999999");
        tenant.Branding.PortalContent!.HeroSecondaryCta.Should().Be("Agendar consulta");
    }

    [Fact]
    public async Task Handle_ShouldRejectStatsFields_WhenDefaultTheme()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "default" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        null, null, null, null, null, null, null,
                        Stat1Label: "Imóveis",
                        Stat1Value: "100+"))),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldPreserveExistingNewFields_WhenPatchIsNull()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "premium" });
        tenant.UpdateBranding(tenant.Branding with
        {
            PortalContent = new PortalBrandingContent
            {
                Stat1Label = "Imóveis",
                Stat1Value = "500+",
                WhatsappNumber = "+5511999999999"
            },
        });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(null, "#123456", "#abcdef", "A", null, null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Branding.PortalContent.Should().NotBeNull();
        tenant.Branding.PortalContent!.Stat1Label.Should().Be("Imóveis");
        tenant.Branding.PortalContent!.WhatsappNumber.Should().Be("+5511999999999");
    }

    [Fact]
    public async Task Handle_MinimalThemeShouldAcceptStatsAndWhatsapp()
    {
        var tenant = Tenant.Create("A", "a", "a@a.com");
        tenant.UpdateEntitlements(new Dictionary<string, object> { ["portal_theme"] = "minimal" });

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(tenant.Id);
        _tenantReadRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateCurrentTenantBrandingCommand(
                new UpdateTenantBrandingRequest(
                    null,
                    "#111111",
                    "#222222",
                    "A",
                    null,
                    null,
                    new PortalBrandingContentResponse(
                        "https://x.test/h.jpg",
                        "Headline",
                        null, null, null, null, null,
                        Stat1Label: "Imóveis",
                        Stat1Value: "500+",
                        WhatsappNumber: "+5511999999999"))),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Branding.PortalContent!.Stat1Label.Should().Be("Imóveis");
        tenant.Branding.PortalContent!.WhatsappNumber.Should().Be("+5511999999999");
    }
}
