using Idempwanna.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Idempwanna.Core.Implementations;

/// <summary>
/// Redis implementation of the idempotency cache
/// </summary>
public class RedisIdempotencyCache : IIdempotencyCache, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _defaultExpirationTime;
    private readonly JsonSerializerOptions _serializerOptions;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the Redis idempotency cache
    /// </summary>
    /// <param name="connectionString">The Redis connection string</param>
    /// <param name="defaultExpirationTime">Default expiration time for cached items</param>
    public RedisIdempotencyCache(string connectionString, TimeSpan? defaultExpirationTime = null)
        : this(ConnectionMultiplexer.Connect(connectionString), defaultExpirationTime)
    {
    }

    /// <summary>
    /// Creates a new instance of the Redis idempotency cache
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer</param>
    /// <param name="defaultExpirationTime">Default expiration time for cached items</param>
    public RedisIdempotencyCache(IConnectionMultiplexer redis, TimeSpan? defaultExpirationTime = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _defaultExpirationTime = defaultExpirationTime ?? TimeSpan.FromHours(24);
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<(bool Found, TResponse? Response)> GetAsync<TResponse>(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(GetFullKey(idempotencyKey));

        if (value.IsNull)
            return (false, default);

        try
        {
            var response = JsonSerializer.Deserialize<TResponse>(value.ToString(), _serializerOptions);
            return (true, response);
        }
        catch (JsonException ex)
        {
            // Log error or handle deserialization failure
            return (false, default);
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<TResponse>(string idempotencyKey, TResponse response, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var db = _redis.GetDatabase();
        var serializedResponse = JsonSerializer.Serialize(response, _serializerOptions);
        
        await db.StringSetAsync(
            GetFullKey(idempotencyKey),
            serializedResponse,
            expiry: expirationTime ?? _defaultExpirationTime
        );
    }

    /// <summary>
    /// Gets the full Redis key with a prefix for the idempotency cache
    /// </summary>
    private static string GetFullKey(string idempotencyKey) => $"idempotency:{idempotencyKey}";

    /// <summary>
    /// Disposes the Redis connection
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_redis is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}