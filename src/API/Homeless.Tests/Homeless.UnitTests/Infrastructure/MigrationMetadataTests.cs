using System.Reflection;
using FluentAssertions;
using Homeless.Infrastructure.Migrations;
using Homeless.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public class MigrationMetadataTests
{
    [Fact]
    public void AddTenantBillingColumns_ShouldExposeEfMigrationMetadata()
    {
        var migrationType = typeof(AddTenantBillingColumns);

        var migrationAttribute = migrationType.GetCustomAttribute<MigrationAttribute>();
        var dbContextAttribute = migrationType.GetCustomAttribute<DbContextAttribute>();

        migrationAttribute.Should().NotBeNull();
        migrationAttribute!.Id.Should().Be("20260311210000_AddTenantBillingColumns");
        dbContextAttribute.Should().NotBeNull();
        dbContextAttribute!.ContextType.Should().Be(typeof(ApplicationDbContext));
    }
}
