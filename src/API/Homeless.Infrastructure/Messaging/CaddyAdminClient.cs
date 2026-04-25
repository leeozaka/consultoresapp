using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class CaddyAdminClient(
    IHttpClientFactory httpClientFactory,
    IOptions<CaddyOptions> options,
    ILogger<CaddyAdminClient> logger)
    : ICaddyAdminClient
{
    private readonly CaddyOptions _options = options.Value;

    public async Task UpsertTenantRouteAsync(
        string routeId,
        IReadOnlyCollection<string> hosts,
        CaddyUpstreamTarget upstream,
        CancellationToken cancellationToken = default)
    {
        if (hosts.Count == 0)
            return;

        var client = CreateClient();

        await RemoveTenantRouteAsync(routeId, cancellationToken).ConfigureAwait(false);

        var route = BuildTenantRoute(routeId, hosts, upstream);
        var response = await client.PostAsJsonAsync(
            $"/config/apps/http/servers/{_options.HttpServerName}/routes",
            route,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Caddy route upsert failed ({response.StatusCode}): {body}");
        }

        logger.LogInformation("Caddy route upserted for {RouteId} hosts [{Hosts}]", routeId, string.Join(", ", hosts));
    }

    public async Task RemoveTenantRouteAsync(
        string routeId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"/id/{routeId}", cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException($"Caddy route delete failed ({response.StatusCode}): {body}");
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(nameof(CaddyAdminClient));
        client.BaseAddress = new Uri(_options.AdminUrl);
        return client;
    }

    private object BuildTenantRoute(
        string routeId,
        IReadOnlyCollection<string> hosts,
        CaddyUpstreamTarget upstream)
    {
        var apiDial = _options.ApiUpstream;
        var upstreamDial = $"{upstream.Host}:{upstream.Port}";

        var upstreamHandler = new Dictionary<string, object?>
        {
            ["handler"] = "reverse_proxy",
            ["upstreams"] = new[] { new { dial = upstreamDial } }
        };

        if (upstream.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            upstreamHandler["transport"] = new Dictionary<string, object?>
            {
                ["protocol"] = "http",
                ["tls"] = new Dictionary<string, object?>()
            };
        }

        return new Dictionary<string, object?>
        {
            ["@id"] = routeId,
            ["match"] = new[] { new { host = hosts.ToArray() } },
            ["handle"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["handler"] = "subroute",
                    ["routes"] = new object[]
                    {
                        new
                        {
                            match = new[] { new { path = new[] { "/api/*", "/connect/*", "/.well-known/*" } } },
                            handle = new[]
                            {
                                new
                                {
                                    handler = "reverse_proxy",
                                    upstreams = new[] { new { dial = apiDial } }
                                }
                            }
                        },
                        new
                        {
                            handle = new object[] { upstreamHandler }
                        }
                    }
                }
            }
        };
    }
}
