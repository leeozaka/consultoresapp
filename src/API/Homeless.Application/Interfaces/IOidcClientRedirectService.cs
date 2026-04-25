namespace Homeless.Application.Interfaces;

public interface IOidcClientRedirectService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}
