using System.Text.Json;
using FluentAssertions;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public class TenantSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Tenant_ShouldSerializeAndDeserialize_Successfully()
    {
        // Arrange
        var originalTenant = Tenant.Create(
            name: "Test Agency",
            slug: "test-agency",
            contactEmail: "contact@test.com",
            contactPhone: "+1234567890",
            type: TenantType.Agency,
            frontendOrigin: null);

        // Act
        var json = JsonSerializer.Serialize(originalTenant, SerializerOptions);
        var deserializedTenant = JsonSerializer.Deserialize<Tenant>(json, SerializerOptions);

        // Assert
        deserializedTenant.Should().NotBeNull();
        deserializedTenant!.Name.Should().Be(originalTenant.Name);
        deserializedTenant.Slug.Should().Be(originalTenant.Slug);
        deserializedTenant.ContactEmail.Should().Be(originalTenant.ContactEmail);
        deserializedTenant.ContactPhone.Should().Be(originalTenant.ContactPhone);
        deserializedTenant.Type.Should().Be(originalTenant.Type);
        deserializedTenant.Status.Should().Be(originalTenant.Status);
    }

    [Fact]
    public void Tenant_WithCompleteData_ShouldSerializeAndDeserialize_Successfully()
    {
        // Arrange
        var originalTenant = Tenant.Create(
            name: "Complete Agency",
            slug: "complete-agency",
            contactEmail: "complete@agency.com",
            contactPhone: "+9876543210",
            type: TenantType.Agency,
            frontendOrigin: "https://custom.agency.com");

        originalTenant.Activate();

        // Act
        var json = JsonSerializer.Serialize(originalTenant, SerializerOptions);
        var deserializedTenant = JsonSerializer.Deserialize<Tenant>(json, SerializerOptions);

        // Assert
        deserializedTenant.Should().NotBeNull();
        deserializedTenant!.Id.Should().Be(originalTenant.Id);
        deserializedTenant.Name.Should().Be(originalTenant.Name);
        deserializedTenant.Slug.Should().Be(originalTenant.Slug);
        deserializedTenant.Status.Should().Be(TenantStatus.Active);
        deserializedTenant.FrontendOrigin.Should().Be(originalTenant.FrontendOrigin);
    }

    [Fact]
    public void Tenant_WithEntitlements_ShouldSerializeAndDeserialize_Successfully()
    {
        // Arrange
        var originalTenant = Tenant.Create(
            name: "Premium Agency",
            slug: "premium-agency",
            contactEmail: "premium@agency.com");

        // Act
        var json = JsonSerializer.Serialize(originalTenant, SerializerOptions);
        var deserializedTenant = JsonSerializer.Deserialize<Tenant>(json, SerializerOptions);

        // Assert
        deserializedTenant.Should().NotBeNull();
        deserializedTenant!.Entitlements.Should().NotBeNull();
        deserializedTenant.Entitlements.Should().ContainKey("max_properties");
    }
}
