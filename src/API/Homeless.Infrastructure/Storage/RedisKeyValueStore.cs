using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Storage;

public sealed class RedisKeyValueStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisKeyValueStore> logger) : IKeyValueStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        if (!value.HasValue)
            return default;

        try
        {
            logger.LogDebug("Key-value store get for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await _database.StringSetAsync(key, payload).ConfigureAwait(false);

        logger.LogDebug("Key-value store set for key: {Key}", key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await _database.StringSetAsync(key, payload, expiry).ConfigureAwait(false);

        logger.LogDebug("Key-value store set for key: {Key} (TTL: {Expiry})", key, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
        logger.LogDebug("Key-value store removed for key: {Key}", key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.KeyExistsAsync(key).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, T>> GetByPrefixAsync<T>(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new Dictionary<string, T>();
        var endpoint = _connectionMultiplexer.GetEndPoints().FirstOrDefault();
        if (endpoint is null)
            return result;

        var server = _connectionMultiplexer.GetServer(endpoint);
        foreach (var redisKey in server.Keys(pattern: $"{prefix}*"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = await GetAsync<T>(redisKey!, cancellationToken).ConfigureAwait(false);
            if (value is not null)
                result[redisKey!] = value;
        }

        logger.LogDebug("Key-value store prefix query for: {Prefix}, Found: {Count}", prefix, result.Count);
        return result;
    }

    public async Task<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newValue = await _database.StringIncrementAsync(key, delta).ConfigureAwait(false);
        logger.LogDebug("Key-value store increment for key: {Key}, Delta: {Delta}, NewValue: {NewValue}", key, delta, newValue);
        return newValue;
    }
}
