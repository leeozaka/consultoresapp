namespace Homeless.Application.UseCases.Onboarding;

/// <summary>
/// Event published to the onboarding SSE stream to report signup/provisioning progress.
/// </summary>
public sealed record OnboardingStatusEvent(
    Guid TenantId,

    /// <summary>
    /// One of: "pending_payment", "payment_confirmed", "provisioning", "ready",
    /// "awaiting_approval", "approved".
    /// </summary>
    string Status,

    string Message,

    DateTime OccurredAt = default)
{
    public DateTime OccurredAt { get; init; } = OccurredAt == default ? DateTime.UtcNow : OccurredAt;
}
