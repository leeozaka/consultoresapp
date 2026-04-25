using System.Security.Claims;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.UseCases.Tenants;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;

namespace Homeless.API.Middleware;

/// <summary>
/// Resolves the current tenant from the incoming request's Host header.
/// Extracts the subdomain to find the tenant by slug, or checks the full
/// host for custom domains.
///
/// Must be placed AFTER UseAuthentication() in the middleware pipeline so
/// that SuperAdmin users can be identified via context.User claims.
/// Admin routes (/api/admin/*) bypass tenant resolution.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IOptions<CachingOptions> cachingOptions,
    IOptions<AppOptions> appOptions
)
{
    // Routes exempt from tenant resolution (system-scoped APIs and public bootstrapping endpoints)
    private static readonly string[] BypassPrefixes =
    [
        "/api/admin",
        "/api/auth",
        "/api/webhooks",
        "/api/tenants/by-slug",
        "/api/tenants/resolve",
        "/health",
        "/.well-known",
        "/metrics",
    ];
    private readonly CachingOptions _caching = cachingOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantReadRepository tenantRepository,
        ITenantContext tenantContext,
        ICacheService cacheService
    )
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldBypass(path))
        {
            await next(context);
            return;
        }

        // SuperAdmin operates globally — skip tenant resolution entirely
        if (context.User.IsInRole("SuperAdmin"))
        {
            await next(context);
            return;
        }

        var isApiRequest = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        var isAuthenticatedRequest = context.User.Identity?.IsAuthenticated == true;

        Domain.Entities.Tenant? tenant = null;

        // For authenticated API requests (dashboard/user-context calls), tenant identity
        // should come from the JWT claim rather than host-derived landing resolution.
        if (isApiRequest && isAuthenticatedRequest)
        {
            tenant = await ResolveFromJwtTenantClaimAsync(context, tenantRepository, cacheService);
            tenant ??= await ResolveFromAuthenticatedUserAsync(
                context,
                tenantRepository,
                cacheService
            );
        }

        var host = TenantHostResolver.NormalizeHost(context.Request.Host.Host);

        // Try to resolve tenant by slug (subdomain) first, then by custom domain
        tenant ??= await ResolveTenantAsync(
            host,
            _app.LandingDomain,
            _app.LandingTenantSlug,
            tenantRepository,
            cacheService,
            context.RequestAborted
        );

        // Secondary fallback: for non-authenticated API calls or non-API requests where
        // host resolution failed but a bearer token still carries tenant context.
        tenant ??= await ResolveFromJwtTenantClaimAsync(context, tenantRepository, cacheService);

        if (tenant is null)
        {
            // No tenant found — 404 for API routes, pass through for root domain
            if (isApiRequest)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
                return;
            }

            await next(context);
            return;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not active" });
            return;
        }

        tenantContext.SetTenant(tenant.Id, tenant.Slug);
        await next(context);
    }

    private static bool ShouldBypass(string path) =>
        BypassPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private async Task<Domain.Entities.Tenant?> ResolveFromJwtTenantClaimAsync(
        HttpContext context,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService
    )
    {
        var jwtResult = await context.AuthenticateAsync(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
        );
        var tenantIdClaim = jwtResult.Principal?.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return null;

        return await ResolveByTenantIdAsync(
            tenantId,
            tenantRepository,
            cacheService,
            context.RequestAborted
        );
    }

    private async Task<Domain.Entities.Tenant?> ResolveFromAuthenticatedUserAsync(
        HttpContext context,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService
    )
    {
        var userIdClaim =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return null;

        var userManager = context.RequestServices.GetService<UserManager<ApplicationUser>>();
        if (userManager is null)
            return null;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.TenantId is not Guid tenantId)
            return null;

        return await ResolveByTenantIdAsync(
            tenantId,
            tenantRepository,
            cacheService,
            context.RequestAborted
        );
    }

    private async Task<Domain.Entities.Tenant?> ResolveByTenantIdAsync(
        Guid tenantId,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = CacheKeys.TenantBySlug($"id:{tenantId}");
        var tenant = await cacheService.GetAsync<Domain.Entities.Tenant>(
            cacheKey,
            cancellationToken
        );
        if (tenant is not null)
            return tenant;

        tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return null;

        var tenantTtl = TimeSpan.FromSeconds(_caching.TenantTtlSeconds);
        await cacheService.SetAsync(cacheKey, tenant, tenantTtl, cancellationToken);
        return tenant;
    }

    private async Task<Domain.Entities.Tenant?> ResolveTenantAsync(
        string host,
        string rootDomain,
        string landingTenantSlug,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService,
        CancellationToken cancellationToken
    )
    {
        var byCustomDomain = await ResolveByCustomDomainAsync(
            host,
            tenantRepository,
            cacheService,
            cancellationToken
        );
        if (byCustomDomain is not null)
            return byCustomDomain;

        // Attempt slug-based resolution (e.g., acme.consultor.app → slug = "acme")
        // Root domain resolves to landing tenant slug.
        var slug = TenantHostResolver.ResolveSlug(host, rootDomain, landingTenantSlug);
        if (string.IsNullOrEmpty(slug))
            return null;

        return await ResolveBySlugAsync(slug, tenantRepository, cacheService, cancellationToken);
    }

    private async Task<Domain.Entities.Tenant?> ResolveByCustomDomainAsync(
        string host,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService,
        CancellationToken cancellationToken
    )
    {
        var tenantTtl = TimeSpan.FromSeconds(_caching.TenantTtlSeconds);
        var cacheKey = CacheKeys.TenantByDomain(host);
        var cached = await cacheService.GetAsync<Domain.Entities.Tenant>(
            cacheKey,
            cancellationToken
        );
        if (cached is not null)
            return cached;

        var tenant = await tenantRepository.GetByCustomDomainAsync(host, cancellationToken);
        if (tenant is not null)
            await cacheService.SetAsync(cacheKey, tenant, tenantTtl, cancellationToken);

        return tenant;
    }

    private async Task<Domain.Entities.Tenant?> ResolveBySlugAsync(
        string slug,
        ITenantReadRepository tenantRepository,
        ICacheService cacheService,
        CancellationToken cancellationToken
    )
    {
        var tenantTtl = TimeSpan.FromSeconds(_caching.TenantTtlSeconds);
        var cacheKey = CacheKeys.TenantBySlug(slug);
        var cached = await cacheService.GetAsync<Domain.Entities.Tenant>(
            cacheKey,
            cancellationToken
        );
        if (cached is not null)
            return cached;

        var tenant = await tenantRepository.GetBySlugAsync(slug, cancellationToken);
        if (tenant is not null)
            await cacheService.SetAsync(cacheKey, tenant, tenantTtl, cancellationToken);

        return tenant;
    }
}
