using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Plans;
using Xunit;

namespace Homeless.UnitTests.Application;

public class GetPlansHandlerTests
{
    private readonly IPlanQueryService _planQueryService = Substitute.For<IPlanQueryService>();
    private readonly GetPlansHandler _handler;

    public GetPlansHandlerTests()
    {
        _handler = new GetPlansHandler(_planQueryService);
    }

    [Fact]
    public async Task Handle_ActiveOnly_ReturnsOnlyActivePlans()
    {
        IReadOnlyList<PlanResponse> plans = [BuildPlanResponse("Starter", 99m), BuildPlanResponse("Pro", 299m)];
        _planQueryService.GetActivePlansAsync(Arg.Any<CancellationToken>()).Returns(plans);

        var result = await _handler.Handle(new GetPlansQuery(ActiveOnly: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        await _planQueryService.Received(1).GetActivePlansAsync(Arg.Any<CancellationToken>());
        await _planQueryService.DidNotReceive().GetAllPlansAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllPlans_ReturnsAllIncludingInactive()
    {
        IReadOnlyList<PlanResponse> plans = [BuildPlanResponse("Starter", 99m), BuildPlanResponse("Legacy", 49m)];
        _planQueryService.GetAllPlansAsync(Arg.Any<CancellationToken>()).Returns(plans);

        var result = await _handler.Handle(new GetPlansQuery(ActiveOnly: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        await _planQueryService.Received(1).GetAllPlansAsync(Arg.Any<CancellationToken>());
        await _planQueryService.DidNotReceive().GetActivePlansAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoPlanseExist_ReturnsEmptyList()
    {
        _planQueryService.GetActivePlansAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PlanResponse>());

        var result = await _handler.Handle(new GetPlansQuery(ActiveOnly: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsNameAndPriceCorrectly()
    {
        IReadOnlyList<PlanResponse> plans = [BuildPlanResponse("Profissional", 199.90m)];
        _planQueryService.GetActivePlansAsync(Arg.Any<CancellationToken>()).Returns(plans);

        var result = await _handler.Handle(new GetPlansQuery(ActiveOnly: true), CancellationToken.None);

        var dto = result.Value.Single();
        dto.Name.Should().Be("Profissional");
        dto.PricePerMonth.Should().Be(199.90m);
    }

    private static PlanResponse BuildPlanResponse(string name, decimal pricePerMonth) =>
        new(
            Id: Guid.NewGuid(),
            Name: name,
            Description: $"{name} plan",
            PricePerMonth: pricePerMonth,
            CurrencyCode: "BRL",
            MaxProperties: 10,
            VideoUpload: false,
            AiDescriptions: false,
            CustomDomain: false,
            PremiumAnalytics: false,
            PortalTheme: "default",
            StripePriceId: null,
            IsActive: true);
}
