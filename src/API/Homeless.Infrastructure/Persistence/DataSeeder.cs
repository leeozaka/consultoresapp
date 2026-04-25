using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Homeless.Application.Authorization;
using Homeless.Application.Interfaces;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.ValueObjects;
using Homeless.Infrastructure.Identity;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with the minimum required data on startup:
///  - ASP.NET Core Identity roles
///  - A SuperAdmin user
///  - An OpenIddict SPA client application
/// Safe to call multiple times (idempotent).
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await NormalizeLegacyTenantTypeValuesAsync(sp);
            await SeedRolesAsync(sp, logger);
            var landingTenantId = await SeedLandingTenantAsync(sp, configuration, logger);
            await SeedSuperAdminAsync(sp, configuration, landingTenantId, logger);
            await SeedDemoTenantAsync(sp, configuration, logger);
            await SeedCustomTenantAsync(sp, configuration, logger);
            await SeedOpenIddictApplicationAsync(sp, configuration, logger);
            var redirectService = sp.GetRequiredService<IOidcClientRedirectService>();
            await redirectService.SyncAsync();
            await SeedDefaultPlansAsync(sp, logger);
            await AssignDefaultPlansToTenantsAsync(sp, configuration, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw new InvalidOperationException("Database seed failed. Check inner exception for details.", ex);
        }
    }

    private static async Task NormalizeLegacyTenantTypeValuesAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE "tenants"
            SET "Type" = 'agency'
            WHERE "Type" IS NULL OR btrim("Type") = '';
            """);
    }

    // ──────────────────────────────────────────────────────────────────────
    private static async Task SeedRolesAsync(IServiceProvider sp, ILogger logger)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in new[] { Roles.SuperAdmin, Roles.TenantAdmin, Roles.Agent })
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (result.Succeeded)
                logger.LogInformation("Created role: {Role}", roleName);
            else
                logger.LogWarning("Failed to create role {Role}: {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    private static async Task SeedSuperAdminAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        Guid landingTenantId,
        ILogger logger)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["SuperAdmin:Email"] ?? "superadmin@consultor.dev";
        var password = configuration["SuperAdmin:Password"] ?? "SuperAdmin@1234!";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.TenantId != landingTenantId)
            {
                existing.TenantId = landingTenantId;
                await userManager.UpdateAsync(existing);
            }

            // Ensure the password stays in sync with config and reset lockout
            if (!await userManager.CheckPasswordAsync(existing, password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                var resetResult = await userManager.ResetPasswordAsync(existing, token, password);
                if (resetResult.Succeeded)
                    logger.LogInformation("Reset SuperAdmin password to match config for {Email}", email);
            }

            if (await userManager.IsLockedOutAsync(existing))
            {
                await userManager.SetLockoutEndDateAsync(existing, null);
                await userManager.ResetAccessFailedCountAsync(existing);
                logger.LogInformation("Cleared SuperAdmin lockout for {Email}", email);
            }

            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            TenantId = landingTenantId
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create SuperAdmin: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        logger.LogInformation("Created SuperAdmin user: {Email}", email);
    }

    // ──────────────────────────────────────────────────────────────────────
    private static async Task<Guid> SeedLandingTenantAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var landingSlug = configuration["App:LandingTenantSlug"] ?? "landing";
        var landingName = configuration["LandingTenant:Name"] ?? "Consultores";
        var landingEmail = configuration["LandingTenant:ContactEmail"] ?? "contato@consultor.dev";

        var existingTenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == landingSlug)
            .ConfigureAwait(false);

        if (existingTenant is not null)
        {
            if (existingTenant.Type != TenantType.System)
                existingTenant.SetType(TenantType.System);

            if (existingTenant.Status is TenantStatus.Pending or TenantStatus.Suspended)
                existingTenant.Activate();

            await db.SaveChangesAsync().ConfigureAwait(false);
            return existingTenant.Id;
        }

        var tenant = Tenant.Create(
            landingName,
            landingSlug,
            landingEmail,
            type: TenantType.System);
        tenant.UpdateEntitlements(new Dictionary<string, object>
        {
            ["max_properties"] = 100000,
            ["video_upload"] = true,
            ["ai_descriptions"] = true,
            ["custom_domain"] = true,
            ["premium_analytics"] = true,
            ["custom_frontend"] = true
        });
        tenant.Activate();

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Seeded landing tenant: {Name} ({Slug})", landingName, landingSlug);
        return tenant.Id;
    }

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Seeds a demonstration tenant with no perks or entitlements enabled.
    /// Idempotent: resets any stray entitlements/addons on every restart so the
    /// demo portal always starts from a clean baseline.
    /// </summary>
    private static async Task SeedDemoTenantAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var tenantName = configuration["DemoTenant:Name"] ?? "Imobiliária Demo";
        var tenantSlug = configuration["DemoTenant:Slug"] ?? "demo";
        var adminEmail = configuration["DemoTenant:AdminEmail"] ?? "admin@demo.consultor.dev";
        var adminPassword = configuration["DemoTenant:AdminPassword"] ?? "Demo@2026!";
        var adminFirstName = configuration["DemoTenant:AdminFirstName"] ?? "Admin";
        var adminLastName = configuration["DemoTenant:AdminLastName"] ?? "Demo";

        // ── Upsert Tenant ────────────────────────────────────────────────────
        var existingTenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug)
            .ConfigureAwait(false);

        Guid tenantId;

        if (existingTenant is null)
        {
            var tenant = Tenant.Create(tenantName, tenantSlug, adminEmail, type: TenantType.Agency);
            tenant.Activate();
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync().ConfigureAwait(false);
            tenantId = tenant.Id;
            logger.LogInformation("Seeded demo tenant: {Name} ({Slug})", tenantName, tenantSlug);
        }
        else
        {
            // Reset to a clean baseline — no plan entitlements
            existingTenant.UpdateEntitlements([]);
            tenantId = existingTenant.Id;
            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        // ── Upsert TenantAdmin user ─────────────────────────────────────────
        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is not null)
        {
            if (!await userManager.CheckPasswordAsync(existingUser, adminPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                await userManager.ResetPasswordAsync(existingUser, token, adminPassword);
                logger.LogInformation("Reset demo TenantAdmin password for {Email}", adminEmail);
            }
            return;
        }

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = adminFirstName,
            LastName = adminLastName,
            IsActive = true,
            TenantId = tenantId
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, Roles.TenantAdmin);
            logger.LogInformation("Seeded demo TenantAdmin: {Email} for tenant {Slug}", adminEmail, tenantSlug);
        }
        else
        {
            logger.LogWarning("Failed to create demo TenantAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Seeds a "custom" tenant whose portalLayoutMode resolves to "Custom" because
    /// <see cref="Tenant.FrontendOrigin"/> is non-null. Accessible locally at
    /// http://custom.consultor.localhost — served by the /features/custom/custom-agency
    /// Angular sub-application (example-agency routes reused as the demo project).
    ///
    /// Run `make setup` after first boot to
    /// add custom.consultor.localhost to /etc/hosts.
    /// </summary>
    private static async Task SeedCustomTenantAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        const string tenantSlug = "custom";
        var landingDomain = configuration["App:LandingDomain"] ?? "consultor.localhost";
        var frontendOrigin = $"custom.{landingDomain}";

        // ── Upsert Tenant ────────────────────────────────────────────────────
        var existingTenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug)
            .ConfigureAwait(false);

        Guid tenantId;

        static Dictionary<string, object> CustomEntitlements() => new()
        {
            ["max_properties"]          = 100,
            ["video_upload"]            = true,
            ["ai_descriptions"]         = true,
            ["custom_domain"]           = true,
            ["premium_analytics"]       = true,
            ["custom_frontend"]         = true,
        };

        if (existingTenant is null)
        {
            var tenant = Tenant.Create(
                name: "Custom Agency Demo",
                slug: tenantSlug,
                contactEmail: "admin@custom.consultor.dev",
                contactPhone: "+5511999999999",
                type: TenantType.Agency,
                frontendOrigin: frontendOrigin);

            tenant.UpdateEntitlements(CustomEntitlements());
            tenant.Activate();
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync().ConfigureAwait(false);
            tenantId = tenant.Id;
            logger.LogInformation(
                "Seeded custom tenant: Custom Agency Demo (custom) with frontendOrigin={Origin}",
                frontendOrigin);
        }
        else
        {
            if (string.IsNullOrEmpty(existingTenant.FrontendOrigin))
                existingTenant.SetFrontendOrigin(frontendOrigin);

            // Always re-apply entitlements so restarts stay in sync with the seed definition
            existingTenant.UpdateEntitlements(CustomEntitlements());
            tenantId = existingTenant.Id;
            await db.SaveChangesAsync().ConfigureAwait(false);
        }

        // ── Upsert TenantAdmin user ─────────────────────────────────────────
        const string adminEmail = "admin@custom.consultor.dev";
        const string adminPassword = "Custom@2026!";

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is not null)
        {
            if (!await userManager.CheckPasswordAsync(existingUser, adminPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                await userManager.ResetPasswordAsync(existingUser, token, adminPassword);
            }
            return;
        }

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "Custom",
            IsActive = true,
            TenantId = tenantId
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, Roles.TenantAdmin);
            logger.LogInformation(
                "Seeded custom TenantAdmin: {Email} for tenant '{Slug}'",
                adminEmail, tenantSlug);
        }
        else
        {
            logger.LogWarning("Failed to create custom TenantAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Ensures that an active <see cref="TenantAddon"/> record exists for every purchasable
    /// perk that should be enabled on the custom demo tenant. Idempotent — existing active
    /// records are left untouched; cancelled records are not re-created (restart-safe).
    /// </summary>
    // removed — addon system

    // ──────────────────────────────────────────────────────────────────────
    private static async Task SeedOpenIddictApplicationAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger)
    {
        var manager = sp.GetRequiredService<IOpenIddictApplicationManager>();

        const string clientId = "consultor-spa";
        var landingDomain = configuration["App:LandingDomain"] ?? "localhost";
        var issuer = configuration["OpenIddict:Issuer"];
        var scheme = Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
            ? issuerUri.Scheme
            : "http";

        var defaultRedirectUri = $"{scheme}://{landingDomain}/auth/callback";
        var defaultPostLogoutUri = $"{scheme}://{landingDomain}";

        var redirectUri = configuration["OpenIddict:SpaRedirectUri"] ?? defaultRedirectUri;
        var postLogoutUri = configuration["OpenIddict:SpaPostLogoutUri"] ?? defaultPostLogoutUri;

        var existing = await manager.FindByClientIdAsync(clientId);
        if (existing is not null)
        {
            // Update redirect URIs so config changes take effect without wiping the DB
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, existing);

            var expectedRedirect = new Uri(redirectUri);
            var expectedPostLogout = new Uri(postLogoutUri);

            var needsUpdate = !descriptor.RedirectUris.Contains(expectedRedirect)
                           || !descriptor.PostLogoutRedirectUris.Contains(expectedPostLogout);

            if (needsUpdate)
            {
                descriptor.RedirectUris.Clear();
                descriptor.RedirectUris.Add(expectedRedirect);
                descriptor.PostLogoutRedirectUris.Clear();
                descriptor.PostLogoutRedirectUris.Add(expectedPostLogout);

                await manager.UpdateAsync(existing, descriptor);
                logger.LogInformation(
                    "Updated OpenIddict SPA redirect URIs → {Redirect}, {PostLogout}",
                    redirectUri, postLogoutUri);
            }

            return;
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            DisplayName = "Consultores SPA",
            RedirectUris = { new Uri(redirectUri) },
            PostLogoutRedirectUris = { new Uri(postLogoutUri) },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Logout,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}api"
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        });

        logger.LogInformation("Created OpenIddict SPA application: {ClientId}", clientId);
    }

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Seeds the perk catalog table with well-known purchasable add-ons.
    /// Idempotent: only inserts perks that do not yet exist (matched by Key).
    /// </summary>
    // removed — addon system

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Seeds the default subscription plans. Idempotent: only inserts plans
    /// that do not yet exist (matched by Name). Existing plans are not modified
    /// so that admin edits are preserved across restarts.
    /// </summary>
    private static async Task SeedDefaultPlansAsync(IServiceProvider sp, ILogger logger)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var defaultPlans = new[]
        {
            Plan.Create("Starter",      "Ideal para corretores autônomos",       Money.Create(2000L,  Currency.BRL), maxProperties: 50, portalTheme: "default"),
            Plan.Create("Profissional", "Para imobiliárias em crescimento",       Money.Create(5900L, Currency.BRL), maxProperties: 500,  aiDescriptions: true, portalTheme: "minimal"),
            Plan.Create("Enterprise",   "Para grandes operações",                 Money.Create(9900L, Currency.BRL), maxProperties: 99999, videoUpload: true, aiDescriptions: true, customDomain: true, premiumAnalytics: true, portalTheme: "premium"),
        };

        foreach (var plan in defaultPlans)
        {
            var exists = await db.Plans.AnyAsync(p => p.Name == plan.Name).ConfigureAwait(false);
            if (exists) continue;

            db.Plans.Add(plan);
            logger.LogInformation("Seeded plan: {Name} @ R${Price}/month", plan.Name, plan.Price.AmountInCents / 100m);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Assigns a default plan to agency tenants that don't have one yet.
    /// Demo tenant gets "Starter", custom tenant gets "Enterprise".
    /// Idempotent: skips tenants that already have a PlanId.
    /// </summary>
    private static async Task AssignDefaultPlansToTenantsAsync(
        IServiceProvider sp,
        IConfiguration configuration,
        ILogger logger)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var starterPlan = await db.Plans
            .FirstOrDefaultAsync(p => p.Name == "Starter")
            .ConfigureAwait(false);

        var enterprisePlan = await db.Plans
            .FirstOrDefaultAsync(p => p.Name == "Enterprise")
            .ConfigureAwait(false);

        if (starterPlan is null || enterprisePlan is null)
        {
            logger.LogWarning("Default plans not found — skipping tenant plan assignment");
            return;
        }

        var demoSlug = configuration["DemoTenant:Slug"] ?? "demo";
        var assignments = new (string Slug, Plan Plan)[]
        {
            (demoSlug, starterPlan),
            ("custom", enterprisePlan),
        };

        foreach (var (slug, plan) in assignments)
        {
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => t.Slug == slug)
                .ConfigureAwait(false);

            if (tenant is null)
                continue;

            tenant.ChangePlan(plan);
            // tenant.RecordPaymentSucceeded(DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
            logger.LogInformation(
                "Ensured plan {Plan} for tenant {Slug}",
                plan.Name, slug);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
