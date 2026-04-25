using Homeless.Application.UseCases.Onboarding;

namespace Homeless.Application.Interfaces;

/// <summary>
/// SSE stream for onboarding lifecycle events (payment confirmed, provisioning, ready, etc.).
/// Tenant-filtered: each SSE connection reads only events for a specific tenant.
/// </summary>
public interface IOnboardingStatusStream
{
    /// <summary>Publishes an event to all subscribers of the event's tenant.</summary>
    ValueTask PublishAsync(OnboardingStatusEvent statusEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an async stream of events for the given tenant.
    /// Replays recent events first, then streams live updates until cancellation.
    /// </summary>
    IAsyncEnumerable<OnboardingStatusEvent> ReadForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
