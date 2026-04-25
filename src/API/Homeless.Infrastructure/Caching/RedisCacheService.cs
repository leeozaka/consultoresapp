using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        // Handle reference loops gracefully in case of circular references
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        if (!value.HasValue)
        {
            logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }

        try
        {
            logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}", key);
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await _database.StringSetAsync(key, payload, expiration).ConfigureAwait(false);

        logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
        logger.LogDebug("Cache removed for key: {Key}", key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.KeyExistsAsync(key).ConfigureAwait(false);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
        return value;
    }
}
