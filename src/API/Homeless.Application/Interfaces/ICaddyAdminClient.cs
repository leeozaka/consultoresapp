namespace Homeless.Application.Interfaces;

public sealed record CaddyUpstreamTarget(
    string Scheme,
    string Host,
    int Port
);

public interface ICaddyAdminClient
{
    Task UpsertTenantRouteAsync(
        string routeId,
        IReadOnlyCollection<string> hosts,
        CaddyUpstreamTarget upstream,
        CancellationToken cancellationToken = default);

    Task RemoveTenantRouteAsync(
        string routeId,
        CancellationToken cancellationToken = default);
}
