using Microsoft.AspNetCore.Cors.Infrastructure;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.API.Middleware;

namespace Homeless.API.Extensions;

public sealed class DynamicCorsPolicyProvider(
    IConfiguration configuration,
    ITenantOriginService tenantOriginService)
    : ICorsPolicyProvider
{
    private static readonly string[] ExposedHeaders =
    [
        CorrelationIdMiddleware.HeaderName,
    ];

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
            return null;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return null;

        var landingDomain = configuration[$"{AppOptions.SectionName}:{nameof(AppOptions.LandingDomain)}"] ?? "localhost";
        var spaOrigin = configuration["OpenIddict:SpaPostLogoutUri"];

        var hostAllowed = originUri.Host.Equals(landingDomain, StringComparison.OrdinalIgnoreCase)
                          || originUri.Host.EndsWith($".{landingDomain}", StringComparison.OrdinalIgnoreCase)
                          || (!string.IsNullOrWhiteSpace(spaOrigin) && origin.Equals(spaOrigin, StringComparison.OrdinalIgnoreCase))
                          || await tenantOriginService.IsAllowedOriginAsync(origin, context.RequestAborted).ConfigureAwait(false);

        if (!hostAllowed)
            return null;

        return new CorsPolicyBuilder()
            .WithOrigins(origin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders(ExposedHeaders)
            .SetPreflightMaxAge(TimeSpan.FromHours(1))
            .Build();
    }
}
