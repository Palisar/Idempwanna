using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Idempwanna.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Idempwanna.Core.Implementations;

/// <summary>
/// In-memory implementation of the idempotency cache
/// </summary>
public class InMemoryIdempotencyCache : IIdempotencyCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryIdempotencyCache> _logger;

    public InMemoryIdempotencyCache(ILogger<InMemoryIdempotencyCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<(bool Found, TResponse? Response)> GetAsync<TResponse>(
        string idempotencyKey, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));
        }

        if (_cache.TryGetValue(idempotencyKey, out var entry))
        {
            // Check if entry has expired
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Cache entry for key {IdempotencyKey} has expired", idempotencyKey);
                _cache.TryRemove(idempotencyKey, out _);
                return Task.FromResult<(bool, TResponse?)>((false, default));
            }

            _logger.LogDebug("Cache hit for key {IdempotencyKey}", idempotencyKey);
            
            if (entry.Value is TResponse response)
            {
                return Task.FromResult((true, response));
            }
            else
            {
                _logger.LogWarning(
                    "Type mismatch for cached value. Expected {ExpectedType} but got {ActualType}",
                    typeof(TResponse).Name,
                    entry.Value?.GetType().Name ?? "null");
                
                return Task.FromResult<(bool, TResponse?)>((false, default));
            }
        }

        _logger.LogDebug("Cache miss for key {IdempotencyKey}", idempotencyKey);
        return Task.FromResult<(bool, TResponse?)>((false, default));
    }

    /// <inheritdoc />
    public Task SetAsync<TResponse>(
        string idempotencyKey, 
        TResponse response, 
        TimeSpan? expirationTime = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));
        }

        DateTimeOffset? expiresAt = expirationTime.HasValue 
            ? DateTimeOffset.UtcNow.Add(expirationTime.Value) 
            : null;

        _cache[idempotencyKey] = new CacheEntry
        {
            Value = response,
            ExpiresAt = expiresAt
        };

        _logger.LogDebug(
            "Added item to cache with key {IdempotencyKey}{ExpirationInfo}", 
            idempotencyKey,
            expiresAt.HasValue ? $", expires at {expiresAt}" : "");

        return Task.CompletedTask;
    }

    private class CacheEntry
    {
        public object? Value { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
    }
}
