using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Homeless.Application.Interfaces;
using Homeless.Domain.Enums;

namespace Homeless.Infrastructure.Auth;

public sealed class TenantOriginService(
    IServiceScopeFactory scopeFactory,
    ILogger<TenantOriginService> logger)
    : ITenantOriginService
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(5);
    private HashSet<string> _allowedHosts = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastRefreshUtc = DateTime.MinValue;

    public async Task<bool> IsAllowedOriginAsync(string origin, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        await EnsureFreshAsync(cancellationToken).ConfigureAwait(false);
        return _allowedHosts.Contains(uri.Host);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var tenantQueryService = scope.ServiceProvider.GetRequiredService<ITenantQueryService>();
            var activeTenants = await tenantQueryService
                .GetByStatusAsync(TenantStatus.Active, cancellationToken)
                .ConfigureAwait(false);

            var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tenant in activeTenants)
            {
                if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
                    hosts.Add(tenant.CustomDomain.Trim().ToLowerInvariant());

                if (!string.IsNullOrWhiteSpace(tenant.FrontendOrigin) &&
                    Uri.TryCreate(tenant.FrontendOrigin, UriKind.Absolute, out var frontendUri))
                {
                    hosts.Add(frontendUri.Host.ToLowerInvariant());
                }
            }

            _allowedHosts = hosts;
            _lastRefreshUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh tenant origin allowlist.");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task EnsureFreshAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastRefreshUtc <= _refreshInterval)
            return;

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }
}
