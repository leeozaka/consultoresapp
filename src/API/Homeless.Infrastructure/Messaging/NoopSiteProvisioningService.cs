using Microsoft.Extensions.Logging;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class NoopSiteProvisioningService(ILogger<NoopSiteProvisioningService> logger) : ISiteProvisioningService
{
    public Task ValidateDnsAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SiteBuild] Validating DNS for tenant {TenantSlug} ({CustomDomain})",
            message.TenantSlug,
            message.CustomDomain ?? "subdomain-only");
        return Task.CompletedTask;
    }

    public Task ProvisionCaddyAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SiteBuild] Skipping external site routing provisioning for tenant {TenantSlug}", message.TenantSlug);
        return Task.CompletedTask;
    }

    public Task ProvisionDefaultContentAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SiteBuild] Provisioning default content for tenant {TenantSlug}", message.TenantSlug);
        return Task.CompletedTask;
    }
}
