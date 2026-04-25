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

public sealed class SyncPlanStripeCatalogHandlerTests
{
    private readonly IPlanReadRepository _planReadRepository = Substitute.For<IPlanReadRepository>();
    private readonly IPlanWriteRepository _planWriteRepository = Substitute.For<IPlanWriteRepository>();
    private readonly IStripePlanCatalogGateway _stripePlanCatalogGateway = Substitute.For<IStripePlanCatalogGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SyncPlanStripeCatalogHandler CreateHandler() =>
        new(
            _planReadRepository,
            _planWriteRepository,
            _stripePlanCatalogGateway,
            _unitOfWork);

    [Fact]
    public async Task Handle_ShouldBackfillMissingStripePriceId()
    {
        var plan = Plan.Create(
            name: "Starter",
            description: "Starter plan",
            price: Money.Create(9900L, Currency.BRL),
            maxProperties: 25);

        _planReadRepository.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(plan);

        _stripePlanCatalogGateway.UpsertPlanCatalogAsync(
                Arg.Any<PlanStripeCatalogRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PlanStripeCatalogResult(
                StripePriceId: "price_starter_v1",
                StripeProductId: "prod_starter",
                CreatedProduct: true,
                CreatedPrice: true));

        var handler = CreateHandler();

        var result = await handler.Handle(new SyncPlanStripeCatalogCommand(plan.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StripePriceId.Should().Be("price_starter_v1");
        plan.StripePriceId.Should().Be("price_starter_v1");

        await _planWriteRepository.Received(1).UpdateAsync(plan, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
