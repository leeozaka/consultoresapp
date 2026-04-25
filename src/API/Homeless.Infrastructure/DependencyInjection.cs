using System.Security.Cryptography.X509Certificates;
using Amazon.Runtime;
using Amazon.S3;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Application.Sagas.Payment;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Infrastructure.Auth;
using Homeless.Infrastructure.Caching;
using Homeless.Infrastructure.Concurrency;
using Homeless.Infrastructure.Entitlements;
using Homeless.Infrastructure.Identity;
using Homeless.Infrastructure.Messaging;
using Homeless.Infrastructure.Metrics;
using Homeless.Infrastructure.Payments;
using Homeless.Infrastructure.Persistence;
using Homeless.Infrastructure.Persistence.Context;
using Homeless.Infrastructure.Persistence.Repositories.Read;
using Homeless.Infrastructure.Persistence.Repositories.Write;
using Homeless.Infrastructure.Resilience;
using Homeless.Infrastructure.Sagas;
using Homeless.Infrastructure.Sagas.Consumers;
using Homeless.Infrastructure.Storage;
using Homeless.Infrastructure.Storage.Options;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Quartz;
using StackExchange.Redis;

namespace Homeless.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));
        services.Configure<ConcurrencyOptions>(
            configuration.GetSection(ConcurrencyOptions.SectionName)
        );
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var writeConnectionString = configuration.GetConnectionString("DefaultConnection");
        var readConnectionString = configuration.GetConnectionString("ReadConnection");
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var rabbitMqOptions =
            configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        if (string.IsNullOrEmpty(readConnectionString))
            readConnectionString = writeConnectionString;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(writeConnectionString))
            {
                options.UseNpgsql(
                    writeConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
                );
            }
            else
            {
                options.UseInMemoryDatabase("HomelessDb");
            }

            options.UseOpenIddict();
        });

        services.AddDbContext<ReadDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(readConnectionString))
            {
                options.UseNpgsql(
                    readConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(15);
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
                );
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
            else
            {
                options.UseInMemoryDatabase("HomelessDb");
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        });

        services.AddScoped<IWriteDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<ITenantOriginService, TenantOriginService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Read repositories (command handlers: entity loading, existence checks)
        services.AddScoped<IPropertyReadRepository, PropertyReadRepository>();
        services.AddScoped<ITenantReadRepository, TenantReadRepository>();
        services.AddScoped<IPlanReadRepository, PlanReadRepository>();
        // Query services (query handlers: DTO projections via ReadDbContext)
        services.AddScoped<IPropertyQueryService, PropertyQueryService>();
        services.AddScoped<ITenantQueryService, TenantQueryService>();
        services.AddScoped<IPlanQueryService, PlanQueryService>();
        // Write repositories
        services.AddScoped<IPropertyWriteRepository, PropertyWriteRepository>();
        services.AddScoped<ITenantWriteRepository, TenantWriteRepository>();
        services.AddScoped<IPlanWriteRepository, PlanWriteRepository>();

        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IUserCommandService, UserCommandService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // Create the multiplexer once — DataProtection and StackExchange cache
            // both close over this instance so no second connection is established.
            var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddSingleton<IKeyValueStore, RedisKeyValueStore>();
            services.AddSingleton<IImageProcessedStream, RedisImageProcessedStream>();
            services.AddSingleton<IPaymentEventStream, RedisPaymentEventStream>();
            services.AddSingleton<IOnboardingStatusStream, RedisOnboardingStatusStream>();

            // Configure Data Protection to persist keys in Redis so authentication cookies
            // remain valid across pod restarts and can be shared across multiple instances.
            services
                .AddDataProtection()
                .SetApplicationName("ConsultoresApp")
                .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys");

            // Configure distributed cache for ASP.NET Core framework features.
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () =>
                    Task.FromResult<IConnectionMultiplexer>(multiplexer);
                options.InstanceName = "ConsultoresApp:";
            });
        }
        else
        {
            // ⚠ WARN: Redis is not configured. Falling back to in-memory implementations.
            // Distributed locking, caching, and key-value store will NOT work across multiple
            // application replicas. This is only safe for single-instance local development.
            services.AddSingleton<IStartupFilter, RedisUnavailableStartupWarning>();
            services.AddSingleton<IDistributedLockService, InMemoryDistributedLockService>();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IKeyValueStore, InMemoryKeyValueStore>();
            services.AddSingleton<IImageProcessedStream, InMemoryImageProcessedStream>();
            services.AddSingleton<IPaymentEventStream, InMemoryPaymentEventStream>();
            services.AddSingleton<IOnboardingStatusStream, InMemoryOnboardingStatusStream>();

            // In-memory data protection (dev only)
            services.AddDataProtection().SetApplicationName("ConsultoresApp");

            // In-memory distributed cache (dev only)
            services.AddDistributedMemoryCache();
        }

        if (rabbitMqOptions.IsConfigured)
            services.AddScoped<IEventBus, MassTransitEventBus>();
        else
            services.AddSingleton<IEventBus, InMemoryEventBus>();

        services.AddSingleton<IMetricsService, MetricsService>();

        services.Configure<ImageProcessingOptions>(
            configuration.GetSection(ImageProcessingOptions.SectionName)
        );

        services.AddSingleton<IImageProcessingChannel, ImageProcessingChannel>();
        services.AddHostedService<ImageProcessingBackgroundService>();

        services.Configure<SiteBuildOptions>(
            configuration.GetSection(SiteBuildOptions.SectionName)
        );
        services.AddSingleton<ISiteBuildChannel, SiteBuildChannel>();
        services.AddSingleton<ISiteBuildStatusStream, InMemorySiteBuildStatusStream>();
        services.AddScoped<ISiteProvisioningService, NoopSiteProvisioningService>();
        services.AddScoped<IOidcClientRedirectService, OidcClientRedirectService>();
        services.AddHostedService<SiteBuildBackgroundService>();

        services.Configure<R2Options>(configuration.GetSection(R2Options.SectionName));
        services.Configure<LocalStorageOptions>(
            configuration.GetSection(LocalStorageOptions.SectionName)
        );

        var r2Opts = configuration.GetSection(R2Options.SectionName).Get<R2Options>();

        if (r2Opts?.IsConfigured == true)
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var credentials = new BasicAWSCredentials(r2Opts.AccessKey, r2Opts.SecretKey);
                var config = new AmazonS3Config
                {
                    ServiceURL = r2Opts.ResolvedServiceUrl,
                    ForcePathStyle = true,
                };
                return new AmazonS3Client(credentials, config);
            });

            services.AddScoped<IStorageService, CloudflareR2StorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LocalFileStorageService>();
        }

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password policy: min 8 chars, at least one uppercase, lowercase, and digit.
                // Non-alphanumeric characters are optional to keep UX friendly.
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                // Lockout: 5 failed attempts → 15-minute lockout.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Register OpenIddict certificate store for Redis-backed certificate caching
        // This allows multiple replicas to share the same signing/encryption certificates
        services.AddSingleton<OpenIddictCertificateStore>();

        var spaOrigin = configuration["OpenIddict:SpaPostLogoutUri"] ?? "http://localhost:4200";
        services.ConfigureApplicationCookie(options =>
        {
            // Cookie settings for distributed authentication
            options.Cookie.Name = "ConsultoresApp.Auth";
            options.Cookie.HttpOnly = true;
            // OIDC redirects can arrive from white-label/custom domains, so Lax is
            // required for the auth cookie on top-level navigation back to the issuer.
            options.Cookie.SameSite = SameSiteMode.Lax;
            // Always require HTTPS in production/staging; allow HTTP only in development.
            options.Cookie.SecurePolicy =
                environment.IsProduction() || environment.IsStaging()
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

            // Sliding expiration keeps users logged in as long as they're active
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);

            options.Events.OnRedirectToLogin = context =>
            {
                if (
                    context.Request.Path.StartsWithSegments(
                        "/api",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                // Always use the full request URL so the SPA redirects back to
                // the *backend* authorize endpoint, not the SPA's own origin.
                var returnUrl = context.Request.GetEncodedUrl();
                var loginUrl =
                    $"{spaOrigin}/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(loginUrl);
                return Task.CompletedTask;
            };
        });

        services
            .AddOpenIddict()
            .AddCore(opts =>
            {
                opts.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
            })
            .AddServer(opts =>
            {
                opts.SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetLogoutEndpointUris("/connect/logout")
                    .SetUserinfoEndpointUris("/connect/userinfo");

                opts.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

                opts.AllowRefreshTokenFlow();

                opts.RegisterScopes(
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    "api"
                );

                var issuer = configuration["OpenIddict:Issuer"];
                if (!string.IsNullOrWhiteSpace(issuer))
                    opts.SetIssuer(new Uri(issuer));

                var requireRealCerts =
                    environment.IsProduction() || environment.EnvironmentName is "Staging";

                if (requireRealCerts)
                {
                    // Load X.509 certificates from environment variables (base64-encoded PFX).
                    // These are injected via Kubernetes Secrets — see infra/k8s/secrets.yaml.example.
                    var signingCertB64 = Environment.GetEnvironmentVariable(
                        "OPENIDDICT_SIGNING_CERT_PFX_B64"
                    );
                    var encryptionCertB64 = Environment.GetEnvironmentVariable(
                        "OPENIDDICT_ENCRYPTION_CERT_PFX_B64"
                    );

                    if (!string.IsNullOrEmpty(signingCertB64))
                        opts.AddSigningCertificate(
                            new X509Certificate2(Convert.FromBase64String(signingCertB64))
                        );
                    else
                        throw new InvalidOperationException(
                            "OPENIDDICT_SIGNING_CERT_PFX_B64 must be set in production/staging."
                        );

                    if (!string.IsNullOrEmpty(encryptionCertB64))
                        opts.AddEncryptionCertificate(
                            new X509Certificate2(Convert.FromBase64String(encryptionCertB64))
                        );
                    else
                        throw new InvalidOperationException(
                            "OPENIDDICT_ENCRYPTION_CERT_PFX_B64 must be set in production/staging."
                        );
                }
                else
                {
                    // Development & Docker: use Redis-backed certificates for multi-replica deployments.
                    // This ensures all pods use the same signing/encryption certificates,
                    // preventing "invalid_grant" errors when tokens are validated across different instances.
                    // Reuses the shared IConnectionMultiplexer singleton when Redis is available.
                    var redisMultiplexer = services
                        .BuildServiceProvider()
                        .GetService<IConnectionMultiplexer>();

                    var certificateStore = new OpenIddictCertificateStore(redisMultiplexer);

                    var signingCert = certificateStore.GetOrCreateSigningCertificate();
                    var encryptionCert = certificateStore.GetOrCreateEncryptionCertificate();

                    opts.AddSigningCertificate(signingCert);
                    opts.AddEncryptionCertificate(encryptionCert);
                }

                var aspNetCore = opts.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableLogoutEndpointPassthrough()
                    .EnableUserinfoEndpointPassthrough();

                // Allow plain HTTP only in local dev (Development, Docker)
                if (environment.IsDevelopment() || environment.EnvironmentName is "Docker")
                    aspNetCore.DisableTransportSecurityRequirement();
            })
            .AddValidation(opts =>
            {
                opts.UseLocalServer();
                opts.UseAspNetCore();
            });

        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        var stripeOpts =
            configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>()
            ?? new StripeOptions();
        services.AddHttpClient(
            "Stripe",
            client =>
            {
                client.BaseAddress = new Uri(stripeOpts.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }
        );

        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IStripePlanCatalogGateway, StripePaymentGateway>();

        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumer<CreateCheckoutSessionConsumer>();
            cfg.AddConsumer<ActivateSubscriptionConsumer>();
            cfg.AddConsumer<PaymentSagaFailedConsumer>();
            cfg.AddQuartzConsumers();

            var paymentSaga = cfg.AddSagaStateMachine<PaymentStateMachine, PaymentSagaState>();

            if (!string.IsNullOrWhiteSpace(writeConnectionString))
            {
                paymentSaga.EntityFrameworkRepository(repository =>
                {
                    repository.ExistingDbContext<ApplicationDbContext>();
                    repository.UsePostgres();
                });
            }
            else
            {
                paymentSaga.InMemoryRepository();
            }

            if (rabbitMqOptions.IsConfigured)
            {
                cfg.UsingRabbitMq(
                    (context, busConfig) =>
                    {
                        busConfig.Host(
                            rabbitMqOptions.Host,
                            rabbitMqOptions.Port,
                            rabbitMqOptions.VirtualHost,
                            hostConfig =>
                            {
                                hostConfig.Username(rabbitMqOptions.Username);
                                hostConfig.Password(rabbitMqOptions.Password);
                            }
                        );

                        busConfig.PrefetchCount = rabbitMqOptions.PrefetchCount;
                        busConfig.UseMessageScheduler(new Uri("queue:quartz"));
                        busConfig.ConfigureEndpoints(context);
                    }
                );
            }
            else
            {
                cfg.UsingInMemory(
                    (context, busConfig) =>
                    {
                        busConfig.UseMessageScheduler(new Uri("queue:quartz"));
                        busConfig.ConfigureEndpoints(context);
                    }
                );
            }
        });

        // Quartz scheduler for MassTransit saga Schedule<> timeout support.
        // PostgreSQL persistent store ensures schedules survive pod restarts.
        // Quartz auto-creates its qrtz_* tables on first startup.
        services.AddQuartz(q =>
        {
            if (!string.IsNullOrWhiteSpace(writeConnectionString))
            {
                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.RetryInterval = TimeSpan.FromSeconds(15);
                    store.UsePostgres(pg => pg.ConnectionString = writeConnectionString);
                    store.UseSystemTextJsonSerializer();
                });
            }
            else
            {
                q.UseInMemoryStore();
            }
        });
        services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

        services.AddSingleton<PaymentSagaService>();
        services.AddSingleton<IPaymentSagaService>(sp =>
            sp.GetRequiredService<PaymentSagaService>()
        );

        services.AddResiliencePolicies(configuration);

        return services;
    }
}
