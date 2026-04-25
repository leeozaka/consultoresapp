using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Domain.Enums;

namespace Homeless.Infrastructure.Messaging;

public sealed class CaddyReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<AppOptions> appOptions,
    IOptions<CaddyOptions> caddyOptions,
    ILogger<CaddyReconciliationHostedService> logger)
    : IHostedService
{
    private readonly AppOptions _appOptions = appOptions.Value;
    private readonly CaddyOptions _caddyOptions = caddyOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var tenantQueryService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.ITenantQueryService>();
        var caddyAdminClient = scope.ServiceProvider.GetRequiredService<ICaddyAdminClient>();

        var activeTenants = await tenantQueryService
            .GetByStatusAsync(TenantStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in activeTenants)
        {
            var hosts = new List<string> { $"{tenant.Slug}.{_appOptions.LandingDomain}".ToLowerInvariant() };
            if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
                hosts.Add(tenant.CustomDomain.Trim().ToLowerInvariant());

            if (hosts.Count == 0)
                continue;

            var upstream = ResolveUpstream(tenant.FrontendOrigin);
            var routeId = $"tenant-route-{tenant.Slug.ToLowerInvariant()}";

            try
            {
                await caddyAdminClient.UpsertTenantRouteAsync(routeId, hosts, upstream, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to reconcile Caddy route for tenant {TenantSlug}", tenant.Slug);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private CaddyUpstreamTarget ResolveUpstream(string? frontendOrigin)
    {
        if (!string.IsNullOrWhiteSpace(frontendOrigin) &&
            Uri.TryCreate(frontendOrigin, UriKind.Absolute, out var parsed))
        {
            var resolvedPort = parsed.Port > 0
                ? parsed.Port
                : 80;

            if (parsed.Port <= 0 && parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                resolvedPort = 443;

            return new CaddyUpstreamTarget(parsed.Scheme, parsed.Host, resolvedPort);
        }

        var split = _caddyOptions.DefaultFrontendUpstream.Split(':', 2, StringSplitOptions.TrimEntries);
        var host = split[0];
        var port = split.Length == 2 && int.TryParse(split[1], out var parsedPort) ? parsedPort : 80;
        return new CaddyUpstreamTarget("http", host, port);
    }
}
