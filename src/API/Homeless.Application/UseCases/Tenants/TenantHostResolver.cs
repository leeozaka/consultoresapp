namespace Homeless.Application.UseCases.Tenants;

public static class TenantHostResolver
{
    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;
        var value = host.Trim().ToLowerInvariant();
        var portSeparator = value.IndexOf(':');
        return portSeparator > 0 ? value[..portSeparator] : value;
    }

    public static string? ResolveSlug(string host, string rootDomain, string landingTenantSlug)
    {
        var normalizedHost = NormalizeHost(host);
        var normalizedRoot = NormalizeHost(rootDomain);

        if (string.IsNullOrWhiteSpace(normalizedHost) || string.IsNullOrWhiteSpace(normalizedRoot))
            return null;

        if (normalizedHost.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return landingTenantSlug;

        var rootParts = normalizedRoot.Split('.').Length;
        var hostParts = normalizedHost.Split('.');
        if (hostParts.Length <= rootParts) return null;

        var slug = hostParts[0];
        return !string.IsNullOrWhiteSpace(slug) && slug != "www" ? slug : null;
    }
}
