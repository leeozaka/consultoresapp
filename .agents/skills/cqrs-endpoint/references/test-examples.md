# Test Examples

Concrete patterns for unit tests and integration tests in this project.

---

## Unit Test — Handler

Stack: **xUnit + NSubstitute + FluentAssertions**

```csharp
using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Properties;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Xunit;

namespace Homeless.UnitTests.Application;

public class CreatePropertyHandlerTests
{
    // Arrange: build fakes once per test class
    private readonly IPropertyReadRepository  _readRepo  = Substitute.For<IPropertyReadRepository>();
    private readonly IPropertyWriteRepository _writeRepo = Substitute.For<IPropertyWriteRepository>();
    private readonly IEntitlementService      _entitlement = Substitute.For<IEntitlementService>();
    private readonly ITenantContext           _tenant    = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService      _user      = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork              _uow       = Substitute.For<IUnitOfWork>();
    private readonly CreatePropertyHandler    _handler;

    public CreatePropertyHandlerTests()
    {
        _tenant.TenantId.Returns(Guid.NewGuid());
        _user.IsAuthenticated.Returns(true);
        _user.UserId.Returns(Guid.NewGuid().ToString());

        _handler = new CreatePropertyHandler(
            _readRepo, _writeRepo, _entitlement, _tenant, _user, _uow);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnCreatedProperty()
    {
        // Arrange — no entitlement cap
        _entitlement.GetEntitlementAsync<int?>(
            Arg.Any<Guid>(), "max_properties", Arg.Any<CancellationToken>())
            .Returns((int?)null);

        var command = new CreatePropertyCommand(
            new PropertyWriteData("Cozy Flat", 1_500_00, "São Paulo", "SP",
                PropertyType.Apartment, ListingType.Rent, 2, 1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Cozy Flat");
        await _writeRepo.Received(1).AddAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EntitlementCapReached_ShouldReturnForbidden()
    {
        // Arrange — set cap to 5, already at 5
        _entitlement.GetEntitlementAsync<int?>(
            Arg.Any<Guid>(), "max_properties", Arg.Any<CancellationToken>())
            .Returns((int?)5);

        _readRepo.CountByTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var command = new CreatePropertyCommand(
            new PropertyWriteData("Extra Flat", 800_00, "Campinas", "SP",
                PropertyType.Apartment, ListingType.Sale, 1, 1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        await _writeRepo.DidNotReceive().AddAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
    }
}
```

**Guidelines:**
- Build the handler directly — no need for a DI container in unit tests.
- Use `Substitute.For<T>()` for every dependency.
- Pre-configure shared fake behaviour in the constructor; override per-test only when that test specifically needs it.
- Test the failure paths as carefully as the happy path — they're usually where bugs hide.

---

## Unit Test — Validator

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using Homeless.Application.UseCases.Properties;
using Homeless.Application.Validators;
using Xunit;

namespace Homeless.UnitTests.Application;

public class CreatePropertyValidatorTests
{
    private readonly CreatePropertyValidator _validator = new();

    [Fact]
    public void Validate_EmptyTitle_ShouldHaveError()
    {
        var command = new CreatePropertyCommand(
            new PropertyWriteData(string.Empty, 1_000, "SP", "SP",
                PropertyType.Apartment, ListingType.Rent, 1, 1));

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Data.Title);
    }

    [Fact]
    public void Validate_NegativePrice_ShouldHaveError()
    {
        var command = new CreatePropertyCommand(
            new PropertyWriteData("Title", -1, "SP", "SP",
                PropertyType.Apartment, ListingType.Rent, 1, 1));

        _validator.TestValidate(command)
                  .ShouldHaveValidationErrorFor(x => x.Data.Price);
    }

    [Fact]
    public void Validate_ValidInput_ShouldPassWithoutErrors()
    {
        var command = new CreatePropertyCommand(
            new PropertyWriteData("Good Title", 500_00, "São Paulo", "SP",
                PropertyType.Apartment, ListingType.Rent, 2, 1));

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
```

---

## Integration Test — HTTP Endpoint

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Homeless.Application.DTOs;
using Homeless.IntegrationTests.Fixtures;
using Xunit;

namespace Homeless.IntegrationTests.Scenarios;

public sealed class PropertyEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task CreateProperty_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/properties", new
        {
            title       = "Ocean View Apt",
            price       = 250000,
            city        = "Florianópolis",
            state       = "SC",
            property_type = "apartment",
            listing_type  = "sale",
            bedrooms    = 3,
            bathrooms   = 2
        }, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>(JsonOpts);
        body!.Title.Should().Be("Ocean View Apt");
        body.City.Should().Be("Florianópolis");
    }

    [Fact]
    public async Task SearchProperties_WithCityFilter_ShouldReturnMatchingResults()
    {
        // Seed a property first
        await _client.PostAsJsonAsync("/api/properties", new
        {
            title = "Beach House", price = 500000, city = "Recife", state = "PE",
            property_type = "house", listing_type = "sale", bedrooms = 4, bathrooms = 3
        }, JsonOpts);

        var response = await _client.GetAsync("/api/properties?city=Recife");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<PropertyResponse>>(JsonOpts);
        body!.Items.Should().Contain(p => p.City == "Recife");
    }

    [Fact]
    public async Task CreateProperty_InvalidRequest_ShouldReturn400()
    {
        // title is required
        var response = await _client.PostAsJsonAsync("/api/properties", new
        {
            title = "", price = 100, city = "SP", state = "SP",
            property_type = "apartment", listing_type = "rent", bedrooms = 1, bathrooms = 1
        }, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Guidelines:**
- Use `factory.CreateClient()` — the factory boots a real in-memory server against the test database.
- Keep tests black-box: call the HTTP API as a consumer would, assert the HTTP contract.
- Use snake_case keys in the request body to match the API's JSON naming policy.
- Chain a seed call + verify call to test queries that require existing data.
