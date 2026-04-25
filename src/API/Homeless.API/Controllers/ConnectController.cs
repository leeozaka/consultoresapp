using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Homeless.Infrastructure.Identity;

namespace Homeless.API.Controllers;

/// <summary>
/// OpenIddict OIDC token/authorization passthrough endpoints.
/// Routes match those registered in OpenIddict server configuration.
/// </summary>
[ApiController]
public sealed class ConnectController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // The principal is already persisted in the authorization code — validate and sign in
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal is null)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // The token principal uses "sub" (OpenIddict), not ClaimTypes.NameIdentifier,
            // so we look up by ID directly instead of using GetUserAsync.
            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            var user = userId is not null ? await userManager.FindByIdAsync(userId) : null;
            if (user is null || !user.IsActive)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var identity = await BuildClaimsIdentityAsync(user, result.Principal.GetScopes());
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        // If the user is not authenticated, challenge (redirect to login)
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.HasFormContentType ? [.. Request.Form] : [.. Request.Query]) });
        }

        var user = await userManager.GetUserAsync(result.Principal!);
        if (user is null)
            return Challenge(IdentityConstants.ApplicationScheme);

        if (!user.IsActive)
        {
            await signInManager.SignOutAsync();
            return Redirect("/login?error=account_pending");
        }

        var scopes = request.GetScopes();
        var identity = await BuildClaimsIdentityAsync(user, scopes);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return SignOut(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, IdentityConstants.ApplicationScheme],
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = user.Id.ToString(),
            [OpenIddictConstants.Claims.Email] = user.Email!,
            [OpenIddictConstants.Claims.Name] = $"{user.FirstName} {user.LastName}".Trim(),
            ["given_name"] = user.FirstName,
            ["family_name"] = user.LastName
        };

        if (user.TenantId.HasValue)
            claims["tenant_id"] = user.TenantId.Value.ToString();

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
            claims[OpenIddictConstants.Claims.Role] = roles;

        return Ok(claims);
    }

    private async Task<ClaimsIdentity> BuildClaimsIdentityAsync(ApplicationUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email!);
        identity.SetClaim(OpenIddictConstants.Claims.Name, $"{user.FirstName} {user.LastName}".Trim());
        identity.SetClaim(OpenIddictConstants.Claims.GivenName, user.FirstName);
        identity.SetClaim(OpenIddictConstants.Claims.FamilyName, user.LastName);

        if (user.TenantId.HasValue)
            identity.SetClaim("tenant_id", user.TenantId.Value.ToString());

        var roles = await userManager.GetRolesAsync(user);
        identity.SetClaims(OpenIddictConstants.Claims.Role, [.. roles]);

        var scopeList = scopes.ToImmutableArray();
        identity.SetScopes(scopeList);
        identity.SetResources(await GetResourcesForScopes(scopeList));
        identity.SetDestinations(GetDestinations);

        return identity;
    }

    private static Task<IEnumerable<string>> GetResourcesForScopes(IEnumerable<string> scopes)
    {
        var resources = scopes.Contains("api")
            ? (IEnumerable<string>)["api"]
            : [];
        return Task.FromResult(resources);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.GivenName:
            case OpenIddictConstants.Claims.FamilyName:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Profile) == true)
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Email) == true)
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Role:
            case "tenant_id":
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject?.HasScope(OpenIddictConstants.Scopes.Roles) == true)
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
