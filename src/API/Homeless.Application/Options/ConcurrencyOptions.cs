namespace Homeless.Application.Options;

public sealed class ConcurrencyOptions
{
    public const string SectionName = "Concurrency";

    /// <summary>Default timeout when acquiring a distributed lock (seconds).</summary>
    public int LockDefaultTimeoutSeconds { get; init; } = 30;

    /// <summary>How often the in-memory lock eviction timer runs (seconds).</summary>
    public int LockEvictionIntervalSeconds { get; init; } = 120;

    /// <summary>Maximum idle time before an unused lock entry is evicted (seconds).</summary>
    public int LockMaxIdleSeconds { get; init; } = 300;

    /// <summary>Maximum time to wait for a transfer saga to complete (seconds).</summary>
    public int SagaTimeoutSeconds { get; init; } = 30;
}
