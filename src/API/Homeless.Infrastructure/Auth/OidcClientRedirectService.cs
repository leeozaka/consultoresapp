using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Domain.Enums;

namespace Homeless.Infrastructure.Auth;

public sealed class OidcClientRedirectService(
    IConfiguration configuration,
    IOpenIddictApplicationManager manager,
    ITenantQueryService tenantQueryService,
    ILogger<OidcClientRedirectService> logger)
    : IOidcClientRedirectService
{
    private const string ClientId = "consultor-spa";

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var existing = await manager.FindByClientIdAsync(ClientId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, existing, cancellationToken).ConfigureAwait(false);

        var redirectUris = await BuildRedirectUrisAsync(cancellationToken).ConfigureAwait(false);
        var postLogoutUris = await BuildPostLogoutUrisAsync(cancellationToken).ConfigureAwait(false);

        var needsUpdate = !descriptor.RedirectUris.SetEquals(redirectUris)
                       || !descriptor.PostLogoutRedirectUris.SetEquals(postLogoutUris);

        if (!needsUpdate)
            return;

        descriptor.RedirectUris.Clear();
        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(uri);

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in postLogoutUris)
            descriptor.PostLogoutRedirectUris.Add(uri);

        await manager.UpdateAsync(existing, descriptor, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("OpenIddict SPA redirect URIs synchronized ({Redirects} redirects, {PostLogouts} post-logout URIs).",
            redirectUris.Count, postLogoutUris.Count);
    }

    private async Task<HashSet<Uri>> BuildRedirectUrisAsync(CancellationToken cancellationToken)
    {
        var result = new HashSet<Uri>();
        AddUriIfValid(result, configuration["OpenIddict:SpaRedirectUri"]);

        foreach (var authority in await BuildAuthoritiesAsync(cancellationToken).ConfigureAwait(false))
            AddUriIfValid(result, $"{authority}/auth/callback");

        return result;
    }

    private async Task<HashSet<Uri>> BuildPostLogoutUrisAsync(CancellationToken cancellationToken)
    {
        var result = new HashSet<Uri>();
        AddUriIfValid(result, configuration["OpenIddict:SpaPostLogoutUri"]);

        foreach (var authority in await BuildAuthoritiesAsync(cancellationToken).ConfigureAwait(false))
            AddUriIfValid(result, authority);

        return result;
    }

    private async Task<HashSet<string>> BuildAuthoritiesAsync(CancellationToken cancellationToken)
    {
        var authorities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issuer = configuration["OpenIddict:Issuer"];
        var landingDomain = configuration[$"{AppOptions.SectionName}:{nameof(AppOptions.LandingDomain)}"] ?? "localhost";

        var scheme = Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
            ? issuerUri.Scheme
            : "http";

        authorities.Add($"{scheme}://{landingDomain}".ToLowerInvariant());

        var activeTenants = await tenantQueryService
            .GetByStatusAsync(TenantStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in activeTenants)
        {
            authorities.Add($"{scheme}://{tenant.Slug}.{landingDomain}".ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(tenant.CustomDomain))
                authorities.Add($"{scheme}://{tenant.CustomDomain.Trim().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(tenant.FrontendOrigin) &&
                Uri.TryCreate(tenant.FrontendOrigin, UriKind.Absolute, out var frontendOrigin))
            {
                authorities.Add(frontendOrigin.GetLeftPart(UriPartial.Authority).ToLowerInvariant());
            }
        }

        return authorities;
    }

    private static void AddUriIfValid(ISet<Uri> destination, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, UriKind.Absolute, out var uri))
            destination.Add(uri);
    }
}
