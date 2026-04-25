using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Plans;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Write;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class CreatePlanHandlerTests
{
    private readonly IPlanWriteRepository _planWriteRepository = Substitute.For<IPlanWriteRepository>();
    private readonly IStripePlanCatalogGateway _stripePlanCatalogGateway = Substitute.For<IStripePlanCatalogGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreatePlanHandler CreateHandler() =>
        new(
            _planWriteRepository,
            _stripePlanCatalogGateway,
            _unitOfWork);

    [Fact]
    public async Task Handle_ShouldCreatePlanAndSyncStripeCatalog()
    {
        _stripePlanCatalogGateway.UpsertPlanCatalogAsync(
                Arg.Any<PlanStripeCatalogRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PlanStripeCatalogResult(
                StripePriceId: "price_growth_v1",
                StripeProductId: "prod_growth",
                CreatedProduct: true,
                CreatedPrice: true));

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreatePlanCommand(
                Name: "Growth",
                Description: "Growth plan",
                PricePerMonth: 199,
                MaxProperties: 100,
                VideoUpload: true,
                AiDescriptions: true,
                CustomDomain: true,
                PremiumAnalytics: true,
                PortalTheme: "default",
                StripePriceId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StripePriceId.Should().Be("price_growth_v1");

        await _planWriteRepository.Received(1).AddAsync(
            Arg.Is<Plan>(plan =>
                plan.Name == "Growth" &&
                plan.StripePriceId == "price_growth_v1" &&
                plan.PortalTheme == "default"),
            Arg.Any<CancellationToken>());

        await _stripePlanCatalogGateway.Received(1).UpsertPlanCatalogAsync(
            Arg.Is<PlanStripeCatalogRequest>(request =>
                request.Name == "Growth" &&
                request.PricePerMonth == 199 &&
                request.StripePriceId == null),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
