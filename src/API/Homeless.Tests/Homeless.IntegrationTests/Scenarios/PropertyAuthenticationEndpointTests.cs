using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Homeless.IntegrationTests.Fixtures;
using Xunit;

namespace Homeless.IntegrationTests.Scenarios;

public sealed class PropertyAuthenticationEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("http://consultor.localhost")
    });

    [Fact]
    public async Task CreateProperty_WhenAnonymous_ShouldReturn401InsteadOfRedirecting()
    {
        var response = await _client.PostAsJsonAsync("/api/properties", new
        {
            title = "Apartamento",
            price = 100,
            city = "São Paulo",
            state = "SP",
            property_type = "Apartment",
            listing_type = "Sale",
            bedrooms = 1,
            bathrooms = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }
}
