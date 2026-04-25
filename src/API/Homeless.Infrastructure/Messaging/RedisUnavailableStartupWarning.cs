using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Homeless.Infrastructure.Messaging;

/// <summary>
/// Emits a loud startup warning when Redis is unavailable and the application falls
/// back to in-memory implementations for caching, distributed locking, and key-value store.
/// Multi-replica deployments will NOT function correctly without Redis.
/// </summary>
internal sealed class RedisUnavailableStartupWarning(ILogger<RedisUnavailableStartupWarning> logger)
    : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        logger.LogWarning(
            "⚠ Redis is NOT configured. " +
            "Distributed locking (InMemoryDistributedLockService), caching (InMemoryCacheService), " +
            "and key-value store (InMemoryKeyValueStore) are SINGLE-INSTANCE ONLY. " +
            "Running multiple replicas without Redis will cause cache inconsistency, lock races, " +
            "and SSE events not reaching all connected clients. " +
            "Set the 'ConnectionStrings:Redis' configuration value to enable Redis.");

        return next;
    }
}
