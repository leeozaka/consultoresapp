using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;

namespace Homeless.Infrastructure.Messaging;

public sealed class CaddySiteProvisioningService(
    ICaddyAdminClient caddyAdminClient,
    IOptions<AppOptions> appOptions,
    IOptions<CaddyOptions> caddyOptions,
    ILogger<CaddySiteProvisioningService> logger)
    : ISiteProvisioningService
{
    private readonly AppOptions _appOptions = appOptions.Value;
    private readonly CaddyOptions _caddyOptions = caddyOptions.Value;

    public async Task ValidateDnsAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.CustomDomain))
            return;

        var domain = message.CustomDomain.Trim().ToLowerInvariant();
        if (domain.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain, cancellationToken).ConfigureAwait(false);
            if (addresses.Length == 0)
                throw new InvalidOperationException($"Custom domain '{domain}' has no DNS records.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DNS validation failed for custom domain {CustomDomain}", domain);
            throw new InvalidOperationException($"DNS validation failed for custom domain '{domain}'.", ex);
        }
    }

    public async Task ProvisionCaddyAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        var hosts = BuildHosts(message);
        if (hosts.Count == 0)
            return;

        var upstream = ResolveUpstream(message);
        var routeId = BuildRouteId(message.TenantSlug);

        await caddyAdminClient.UpsertTenantRouteAsync(routeId, hosts, upstream, cancellationToken).ConfigureAwait(false);
    }

    public Task ProvisionDefaultContentAsync(SiteBuildMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Default content provisioning completed for tenant {TenantSlug}", message.TenantSlug);
        return Task.CompletedTask;
    }

    private List<string> BuildHosts(SiteBuildMessage message)
    {
        var hosts = new List<string>();
        if (!string.IsNullOrWhiteSpace(message.TenantSlug))
            hosts.Add($"{message.TenantSlug}.{_appOptions.LandingDomain}".ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(message.CustomDomain))
            hosts.Add(message.CustomDomain.Trim().ToLowerInvariant());

        return hosts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private CaddyUpstreamTarget ResolveUpstream(SiteBuildMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.FrontendOrigin) &&
            Uri.TryCreate(message.FrontendOrigin, UriKind.Absolute, out var uri))
        {
            var port = uri.Port > 0
                ? uri.Port
                : 80;

            if (uri.Port <= 0 && uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                port = 443;

            return new CaddyUpstreamTarget(uri.Scheme, uri.Host, port);
        }

        return ParseDial(_caddyOptions.DefaultFrontendUpstream);
    }

    private static CaddyUpstreamTarget ParseDial(string dial)
    {
        var split = dial.Split(':', 2, StringSplitOptions.TrimEntries);
        var host = split[0];
        var port = split.Length == 2 && int.TryParse(split[1], out var parsed) ? parsed : 80;
        return new CaddyUpstreamTarget("http", host, port);
    }

    private static string BuildRouteId(string slug) => $"tenant-route-{slug.ToLowerInvariant()}";
}
