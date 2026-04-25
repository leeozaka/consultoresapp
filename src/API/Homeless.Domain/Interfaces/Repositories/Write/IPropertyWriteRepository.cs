using Homeless.Domain.Entities;

namespace Homeless.Domain.Interfaces.Repositories.Write;

/// <summary>
/// Write-side property persistence. Implementations use change-tracking or explicit attach.
/// </summary>
public interface IPropertyWriteRepository
{
    ValueTask AddAsync(Property property, CancellationToken cancellationToken = default);
    ValueTask UpdateAsync(Property property, CancellationToken cancellationToken = default);
}
