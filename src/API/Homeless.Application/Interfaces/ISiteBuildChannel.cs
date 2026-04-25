namespace Homeless.Application.Interfaces;

public sealed record SiteBuildMessage(
    Guid TenantId,
    string TenantSlug,
    string? CustomDomain,
    string? FrontendOrigin
);

public interface ISiteBuildChannel
{
    ValueTask WriteAsync(SiteBuildMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SiteBuildMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}
