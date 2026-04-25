namespace Homeless.API.Middleware;

/// <summary>
/// Adds defense-in-depth security headers to every response.
/// The Kubernetes ingress (e.g. NGINX) may also set these, but applying them at the
/// application level ensures coverage for direct pod-to-pod traffic
/// (e.g. SSR server → API) and local development.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        if (context.Request.IsHttps)
        {
            headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        }

        return next(context);
    }
}
