using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Homeless.Application.DTOs;
using Homeless.Application.UseCases.Auth;
using Homeless.Infrastructure.Identity;

namespace Homeless.API.Controllers;

/// <summary>
/// Identity helpers: current user info, registration, and sign-in.
/// OIDC clients should use /connect/authorize instead of the login endpoint.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[TranslateResultToActionResult]
public sealed class AuthController(
    IMediator mediator,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    /// <summary>Returns the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new { user.Id, user.Email, user.FirstName, user.LastName, user.TenantId, Roles = roles });
    }

    /// <summary>
    /// Creates a new user account (orphan — no tenant assigned yet).
    /// A SuperAdmin must later assign the user to a tenant via /api/admin/users/{id}/assign-tenant.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegisteredUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ExpectedFailures(ResultStatus.Error, ResultStatus.Invalid)]
    public async Task<Result<RegisteredUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new RegisterUserCommand(request.Email, request.Password, request.FirstName, request.LastName),
            cancellationToken);

    /// <summary>
    /// Signs in with email + password (direct, for dev tooling only).
    /// Production clients should use the PKCE authorization code flow at /connect/authorize.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials" });

        if (!user.IsActive)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "account_pending", message = "Your account is pending approval. A SuperAdmin must assign you to a tenant before you can sign in." });

        var result = await signInManager.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = "account_locked", message = "Too many failed attempts. Your account is temporarily locked. Please try again later." });

        return result.Succeeded ? Ok(new { message = "Signed in" }) : Unauthorized(new { error = "Invalid credentials" });
    }

    /// <summary>Signs out the current user.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

public sealed record LoginRequest(string Email, string Password);
