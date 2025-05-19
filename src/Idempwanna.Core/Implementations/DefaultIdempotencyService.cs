using System;
using System.Threading;
using System.Threading.Tasks;
using Idempwanna.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Idempwanna.Core.Implementations;

/// <summary>
/// Default implementation of the idempotency service
/// </summary>
public class DefaultIdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyCache _cache;
    private readonly ILogger<DefaultIdempotencyService> _logger;

    public DefaultIdempotencyService(
        IIdempotencyCache cache,
        ILogger<DefaultIdempotencyService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> ProcessAsync<TResponse>(
        string idempotencyKey,
        Func<CancellationToken, Task<TResponse>> executionFunction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key cannot be null, empty, or whitespace", nameof(idempotencyKey));
        }

        // Try to get from cache first
        var (found, cachedResponse) = await _cache.GetAsync<TResponse>(idempotencyKey, cancellationToken);
        
        if (found)
        {
            _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
            return cachedResponse!;
        }

        // Execute the function and cache the result
        _logger.LogInformation("Executing function for idempotency key: {IdempotencyKey}", idempotencyKey);
        var response = await executionFunction(cancellationToken);
        
        await _cache.SetAsync(idempotencyKey, response, null, cancellationToken);
        
        return response;
    }

    /// <inheritdoc />
    public async Task<TResponse> ProcessWithContextAsync<TContext, TResponse>(
        string idempotencyKey,
        TContext context,
        Func<TContext, CancellationToken, Task<TResponse>> executionFunction,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key cannot be null, empty, or whitespace", nameof(idempotencyKey));
        }

        // Try to get from cache first
        var (found, cachedResponse) = await _cache.GetAsync<TResponse>(idempotencyKey, cancellationToken);
        
        if (found)
        {
            _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
            return cachedResponse!;
        }

        // Execute the function with context and cache the result
        _logger.LogInformation("Executing function with context for idempotency key: {IdempotencyKey}", idempotencyKey);
        var response = await executionFunction(context, cancellationToken);
        
        await _cache.SetAsync(idempotencyKey, response, null, cancellationToken);
        
        return response;
    }
}
