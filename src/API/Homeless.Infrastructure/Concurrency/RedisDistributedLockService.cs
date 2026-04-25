using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Homeless.Application.Interfaces;
using Homeless.Application.Options;

namespace Homeless.Infrastructure.Concurrency;

public sealed class RedisDistributedLockService(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<ConcurrencyOptions> options) : IDistributedLockService
{
    private const string ReleaseScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(options.Value.LockDefaultTimeoutSeconds);
    private readonly TimeSpan _leaseTime = TimeSpan.FromSeconds(options.Value.LockMaxIdleSeconds);

    public async Task<IAsyncDisposable> AcquireLockAsync(
        string resource,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var key = $"lock:{resource}";
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow.Add(effectiveTimeout);

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await _database.StringSetAsync(
                    key,
                    token,
                    _leaseTime,
                    when: When.NotExists)
                .ConfigureAwait(false);

            if (acquired)
                return new RedisLockHandle(_database, key, token);

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Could not acquire lock for resource '{resource}' within {effectiveTimeout.TotalSeconds}s.");
    }

    private sealed class RedisLockHandle(IDatabase database, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        private readonly IDatabase _database = database;
        private readonly RedisKey _key = key;
        private readonly RedisValue _token = token;
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _database.ScriptEvaluateAsync(ReleaseScript, [_key], [_token]).ConfigureAwait(false);
            _disposed = true;
        }
    }
}
