using System;
using System.Threading;
using System.Threading.Tasks;

namespace Idempwanna.Core.Interfaces;

/// <summary>
/// Cache for storing responses to idempotent requests
/// </summary>
public interface IIdempotencyCache
{
    /// <summary>
    /// Gets a cached response for the given idempotency key
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    /// <param name="idempotencyKey">The idempotency key</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The cached response or default if not found</returns>
    Task<(bool Found, TResponse? Response)> GetAsync<TResponse>(
        string idempotencyKey, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a response in the cache for the given idempotency key
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    /// <param name="idempotencyKey">The idempotency key</param>
    /// <param name="response">The response to cache</param>
    /// <param name="expirationTime">Optional expiration time</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SetAsync<TResponse>(
        string idempotencyKey, 
        TResponse response, 
        TimeSpan? expirationTime = null, 
        CancellationToken cancellationToken = default);
}
