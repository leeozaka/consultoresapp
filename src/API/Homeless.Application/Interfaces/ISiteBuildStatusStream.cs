using Homeless.Application.DTOs;

namespace Homeless.Application.Interfaces;

public interface ISiteBuildStatusStream
{
    ValueTask PublishAsync(SiteBuildStatusEventResponse statusEvent, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SiteBuildStatusEventResponse> ReadAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
