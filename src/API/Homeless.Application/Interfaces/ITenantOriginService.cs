namespace Homeless.Application.Interfaces;

public interface ITenantOriginService
{
    Task<bool> IsAllowedOriginAsync(string origin, CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
