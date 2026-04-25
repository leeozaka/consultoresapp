using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Plans;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public sealed class UpdatePlanHandlerTests
{
    private readonly IPlanReadRepository _planReadRepository = Substitute.For<IPlanReadRepository>();
    private readonly IPlanWriteRepository _planWriteRepository = Substitute.For<IPlanWriteRepository>();
    private readonly IStripePlanCatalogGateway _stripePlanCatalogGateway = Substitute.For<IStripePlanCatalogGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdatePlanHandler CreateHandler() =>
        new(
            _planReadRepository,
            _planWriteRepository,
            _stripePlanCatalogGateway,
            _unitOfWork);

    [Fact]
    public async Task Handle_ShouldUpdatePlanAndRotateStripePriceWhenBillingChanges()
    {
        var plan = Plan.Create(
            name: "Growth",
            description: "Growth plan",
            price: Money.Create(19900L, Currency.BRL),
            maxProperties: 100,
            stripePriceId: "price_growth_v1");

        _planReadRepository.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(plan);

        _stripePlanCatalogGateway.UpsertPlanCatalogAsync(
                Arg.Any<PlanStripeCatalogRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PlanStripeCatalogResult(
                StripePriceId: "price_growth_v2",
                StripeProductId: "prod_growth",
                CreatedProduct: false,
                CreatedPrice: true));

        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdatePlanCommand(
                PlanId: plan.Id,
                Name: "Growth Plus",
                Description: "Growth plan updated",
                PricePerMonth: 249,
                MaxProperties: 150,
                VideoUpload: true,
                AiDescriptions: true,
                CustomDomain: true,
                PremiumAnalytics: true,
                PortalTheme: "premium",
                StripePriceId: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StripePriceId.Should().Be("price_growth_v2");
        plan.Name.Should().Be("Growth Plus");
        plan.StripePriceId.Should().Be("price_growth_v2");
        plan.PortalTheme.Should().Be("premium");

        await _stripePlanCatalogGateway.Received(1).UpsertPlanCatalogAsync(
            Arg.Is<PlanStripeCatalogRequest>(request =>
                request.PlanId == plan.Id &&
                request.StripePriceId == "price_growth_v1" &&
                request.PricePerMonth == 249),
            Arg.Any<CancellationToken>());

        await _planWriteRepository.Received(1).UpdateAsync(plan, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
