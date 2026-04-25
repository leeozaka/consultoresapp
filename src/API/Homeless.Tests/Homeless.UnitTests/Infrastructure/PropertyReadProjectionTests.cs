using FluentAssertions;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;
using Homeless.Infrastructure.Persistence.Repositories.Read;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public class PropertyReadProjectionTests
{
    [Fact]
    public void ToResponse_ShouldMapScalarFieldsAndImages()
    {
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var projection = new PropertyReadProjection(
            propertyId,
            "Cobertura Duplex",
            "Vista para o mar",
            125_000_000L,
            "BRL",
            "Rio de Janeiro",
            "RJ",
            "BR",
            "22000-000",
            "Av. Atlântica, 100",
            4,
            3,
            2,
            240m,
            PropertyType.House,
            ListingType.Sale,
            PropertyStatus.Active,
            new Dictionary<string, object> { ["HasPool"] = true },
            [
                new()
                {
                    Key = "properties/1/raw.webp",
                    OriginalFileName = "raw.webp",
                    Order = 0,
                    IsProcessed = false
                }
            ]);

        var response = projection.ToResponse();

        response.Title.Should().Be("Cobertura Duplex");
        response.Images.Should().ContainSingle();
        response.Images[0].Key.Should().Be("properties/1/raw.webp");
        response.Images[0].OriginalFileName.Should().Be("raw.webp");
        response.Images[0].IsProcessed.Should().BeFalse();
        response.Attributes.Should().ContainKey("HasPool");
    }
}
