using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Persistence.Context;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Design-time only (<c>dotnet ef</c>). Defaults to local <c>homeless</c> DB (same as docker-compose
    /// <c>POSTGRES_DB</c>). Override with env <c>ConnectionStrings__DefaultConnection</c>.
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseOpenIddict();

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public string TenantSlug => string.Empty;
        public bool IsResolved => false;

        public void SetTenant(Guid tenantId, string slug)
        {
        }
    }
}
