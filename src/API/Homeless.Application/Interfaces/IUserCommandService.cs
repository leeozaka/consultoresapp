namespace Homeless.Application.Interfaces;

/// <summary>
/// Write operations for platform users (ASP.NET Identity abstraction).
/// </summary>
public interface IUserCommandService
{
    /// <summary>
    /// Sets the active flag on a user. Inactive users cannot log in.
    /// </summary>
    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}
