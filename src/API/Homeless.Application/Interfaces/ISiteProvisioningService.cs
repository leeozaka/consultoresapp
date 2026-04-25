namespace Homeless.Application.Interfaces;

public interface ISiteProvisioningService
{
    Task ValidateDnsAsync(SiteBuildMessage message, CancellationToken cancellationToken = default);
    Task ProvisionCaddyAsync(SiteBuildMessage message, CancellationToken cancellationToken = default);
    Task ProvisionDefaultContentAsync(SiteBuildMessage message, CancellationToken cancellationToken = default);
}
