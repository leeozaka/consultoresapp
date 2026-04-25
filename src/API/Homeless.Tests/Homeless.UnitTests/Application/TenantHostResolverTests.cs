using FluentAssertions;
using Homeless.Application.UseCases.Tenants;
using Xunit;

namespace Homeless.UnitTests.Application;

public class TenantHostResolverTests
{
    [Fact]
    public void ResolveSlug_ShouldReturnLandingSlug_WhenHostMatchesRootDomain()
    {
        var slug = TenantHostResolver.ResolveSlug("consultor.localhost", "consultor.localhost", "landing");

        slug.Should().Be("landing");
    }

    [Fact]
    public void ResolveSlug_ShouldReturnTenantSlug_WhenHostHasSubdomain()
    {
        var slug = TenantHostResolver.ResolveSlug("demo.consultor.localhost", "consultor.localhost", "landing");

        slug.Should().Be("demo");
    }

    [Fact]
    public void ResolveSlug_ShouldIgnoreWwwSubdomain()
    {
        var slug = TenantHostResolver.ResolveSlug("www.consultor.localhost", "consultor.localhost", "landing");

        slug.Should().BeNull();
    }

    [Fact]
    public void NormalizeHost_ShouldStripPort()
    {
        var normalized = TenantHostResolver.NormalizeHost("Demo.Consultor.Localhost:8080");

        normalized.Should().Be("demo.consultor.localhost");
    }
}
