using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace Homeless.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                      | ForwardedHeaders.XForwardedHost
                                      | ForwardedHeaders.XForwardedProto;
            // In K8s only the ingress and the SSR Node process forward requests
            // to the API pod. Clear the default limits so the middleware trusts
            // any reverse proxy in the cluster network.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddCors();
        services.AddSingleton<ICorsPolicyProvider, DynamicCorsPolicyProvider>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Consultores API",
                    Version = "v1",
                    Description = "Multi-tenant white-label SaaS API for real estate agencies. " +
                                  "Supports tenant management, property listings, subscription billing via Stripe, " +
                                  "add-on entitlements, and async image processing.",
                    Contact = new OpenApiContact
                    {
                        Name = "Consultores Engineering"
                    }
                };
                return Task.CompletedTask;
            });
        });

        services.AddRateLimiter(limiter =>
        {
            limiter.AddSlidingWindowLimiter("auth", options =>
            {
                options.PermitLimit = 10;
                options.Window = TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = 4;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "rate_limit_exceeded", message = "Too many requests. Please slow down and try again." },
                    token);
            };
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var healthChecksBuilder = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: ["ready"]);
        }

        return services;
    }
}
