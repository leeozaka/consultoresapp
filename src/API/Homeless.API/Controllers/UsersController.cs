using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;
using Homeless.Application.UseCases.Users;

namespace Homeless.API.Controllers;

/// <summary>
/// SuperAdmin user management: list users, resolve orphans, assign to tenants.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.SuperAdmin)]
[Produces("application/json")]
[TranslateResultToActionResult]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all platform users with their tenant assignment status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<UserResponse>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUsersQuery(), cancellationToken);
        return PaginatedResponse<UserResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>Lists users who have no tenant assigned (orphan accounts awaiting onboarding).</summary>
    [HttpGet("orphaned")]
    [ProducesResponseType(typeof(PaginatedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<UserResponse>> GetOrphanedUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetOrphanedUsersQuery(), cancellationToken);
        return PaginatedResponse<UserResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>
    /// Assigns an orphan user to an existing tenant and grants them a role.
    /// Idempotent: if the user is already in a role, the existing role is replaced.
    /// </summary>
    [HttpPost("{userId:guid}/assign-tenant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid, ResultStatus.Error)]
    public async Task<Result> AssignTenant(
        Guid userId,
        [FromBody] AssignTenantRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(new AssignTenantCommand(userId, request.TenantId, request.Role), cancellationToken);

    /// <summary>
    /// Activates or deactivates a user account, controlling their ability to log in.
    /// </summary>
    [HttpPut("{id:guid}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result> ToggleActive(
        Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken) =>
        await mediator.Send(new ToggleUserActiveCommand(id, isActive), cancellationToken);

    /// <summary>Returns a single user by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<UserResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetUserByIdQuery(id), cancellationToken);

    /// <summary>Returns all users belonging to the specified tenant.</summary>
    [HttpGet("by-tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<UserResponse>> GetByTenant(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetUsersByTenantQuery(tenantId), cancellationToken);
        return PaginatedResponse<UserResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>Creates a new user, optionally assigning them to a tenant with a role.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.NotFound, ResultStatus.Error)]
    public async Task<Result<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new CreateUserCommand(request.Email, request.Password, request.FirstName, request.LastName, request.TenantId, request.Role),
            cancellationToken);

    /// <summary>Updates a user's profile (name and/or email).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.NotFound, ResultStatus.Error)]
    public async Task<Result<UserResponse>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new UpdateUserCommand(id, request.FirstName, request.LastName, request.Email),
            cancellationToken);
}
