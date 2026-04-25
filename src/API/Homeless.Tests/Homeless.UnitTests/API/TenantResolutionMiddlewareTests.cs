using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Validation.AspNetCore;
using Homeless.API.Middleware;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;
using Homeless.Domain.Entities;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Infrastructure.Identity;
using Xunit;

namespace Homeless.UnitTests.API;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AuthenticatedApiRequest_ShouldPreferJwtTenantOverHostTenant()
    {
        var landingTenant = Tenant.Create("Landing", "landing", "landing@consultor.dev");
        landingTenant.Activate();

        var customTenant = Tenant.Create("Custom", "custom", "custom@consultor.dev");
        customTenant.Activate();

        var tenantRepository = Substitute.For<ITenantReadRepository>();
        tenantRepository.GetBySlugAsync("landing", Arg.Any<CancellationToken>()).Returns(landingTenant);
        tenantRepository.GetByIdAsync(customTenant.Id, Arg.Any<CancellationToken>()).Returns(customTenant);

        var tenantContext = Substitute.For<ITenantContext>();
        var cacheService = Substitute.For<ICacheService>();
        cacheService.GetAsync<Tenant>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var claimsIdentity = new ClaimsIdentity(
        [
            new Claim("tenant_id", customTenant.Id.ToString())
        ],
        authenticationType: "Bearer");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(claimsIdentity),
            RequestServices = BuildServicesForAuthResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)))
        };

        context.Request.Path = "/api/properties";
        context.Request.Host = new HostString("consultor.localhost");

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new CachingOptions { TenantTtlSeconds = 300 }),
            Options.Create(new AppOptions { LandingDomain = "consultor.localhost", LandingTenantSlug = "landing" }));

        await middleware.InvokeAsync(context, tenantRepository, tenantContext, cacheService);

        nextCalled.Should().BeTrue();
        tenantContext.Received(1).SetTenant(customTenant.Id, "custom");
    }

    [Fact]
    public async Task InvokeAsync_AnonymousApiRequest_ShouldResolveTenantByHost()
    {
        var customTenant = Tenant.Create("Custom", "custom", "custom@consultor.dev");
        customTenant.Activate();

        var tenantRepository = Substitute.For<ITenantReadRepository>();
        tenantRepository.GetBySlugAsync("custom", Arg.Any<CancellationToken>()).Returns(customTenant);

        var tenantContext = Substitute.For<ITenantContext>();
        var cacheService = Substitute.For<ICacheService>();
        cacheService.GetAsync<Tenant>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
            RequestServices = BuildServicesForAuthResult(AuthenticateResult.NoResult())
        };

        context.Request.Path = "/api/properties";
        context.Request.Host = new HostString("custom.consultor.localhost");

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new CachingOptions { TenantTtlSeconds = 300 }),
            Options.Create(new AppOptions { LandingDomain = "consultor.localhost", LandingTenantSlug = "landing" }));

        await middleware.InvokeAsync(context, tenantRepository, tenantContext, cacheService);

        nextCalled.Should().BeTrue();
        tenantContext.Received(1).SetTenant(customTenant.Id, "custom");
    }

    [Fact]
    public async Task InvokeAsync_CookieAuthenticatedApiRequest_ShouldResolveTenantFromUserInsteadOfLandingHost()
    {
        var landingTenant = Tenant.Create("Landing", "landing", "landing@consultor.dev");
        landingTenant.Activate();

        var customTenant = Tenant.Create("Custom", "custom", "custom@consultor.dev");
        customTenant.Activate();

        var tenantRepository = Substitute.For<ITenantReadRepository>();
        tenantRepository.GetBySlugAsync("landing", Arg.Any<CancellationToken>()).Returns(landingTenant);
        tenantRepository.GetByIdAsync(customTenant.Id, Arg.Any<CancellationToken>()).Returns(customTenant);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin@custom.consultor.dev",
            UserName = "admin@custom.consultor.dev",
            TenantId = customTenant.Id,
            IsActive = true
        };

        var userManager = CreateUserManager();
        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);

        var tenantContext = Substitute.For<ITenantContext>();
        var cacheService = Substitute.For<ICacheService>();
        cacheService.GetAsync<Tenant>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var claimsIdentity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email!)
        ],
        authenticationType: IdentityConstants.ApplicationScheme);

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(claimsIdentity),
            RequestServices = BuildServicesForAuthResult(
                AuthenticateResult.NoResult(),
                userManager)
        };

        context.Request.Path = "/api/properties";
        context.Request.Host = new HostString("consultor.localhost");

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new CachingOptions { TenantTtlSeconds = 300 }),
            Options.Create(new AppOptions { LandingDomain = "consultor.localhost", LandingTenantSlug = "landing" }));

        await middleware.InvokeAsync(context, tenantRepository, tenantContext, cacheService);

        nextCalled.Should().BeTrue();
        tenantContext.Received(1).SetTenant(customTenant.Id, "custom");
    }

    [Fact]
    public async Task InvokeAsync_WebhookRoute_ShouldBypassTenantResolution()
    {
        var tenantRepository = Substitute.For<ITenantReadRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var cacheService = Substitute.For<ICacheService>();

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
            RequestServices = BuildServicesForAuthResult(AuthenticateResult.NoResult())
        };

        context.Request.Path = "/api/webhooks/stripe";
        context.Request.Host = new HostString("api");

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Options.Create(new CachingOptions { TenantTtlSeconds = 300 }),
            Options.Create(new AppOptions { LandingDomain = "consultor.localhost", LandingTenantSlug = "landing" }));

        await middleware.InvokeAsync(context, tenantRepository, tenantContext, cacheService);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(StatusCodes.Status404NotFound);
        await tenantRepository.DidNotReceiveWithAnyArgs().GetBySlugAsync(default!, default);
        tenantContext.DidNotReceiveWithAnyArgs().SetTenant(default, default!);
    }

    private static IServiceProvider BuildServicesForAuthResult(
        AuthenticateResult authenticateResult,
        UserManager<ApplicationUser>? userManager = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(new StubAuthenticationService(authenticateResult));

        if (userManager is not null)
            services.AddSingleton(userManager);

        return services.BuildServiceProvider();
    }

    private static UserManager<ApplicationUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();

        return Substitute.For<UserManager<ApplicationUser>>(
            store,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private sealed class StubAuthenticationService(AuthenticateResult authenticateResult) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(authenticateResult);

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
