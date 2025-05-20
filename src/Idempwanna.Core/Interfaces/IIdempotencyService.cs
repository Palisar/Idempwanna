namespace Idempwanna.Core.Interfaces;

/// <summary>
/// Service for handling idempotent operations
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Processes a request with idempotency handling
    /// </summary>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    /// <param name="idempotencyKey">The unique idempotency key for the request</param>
    /// <param name="executionFunction">The function to execute if this is not a duplicate request</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response from either cache or execution</returns>
    Task<TResponse> ProcessAsync<TResponse>(
        string idempotencyKey,
        Func<CancellationToken, Task<TResponse>> executionFunction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a request with idempotency handling and custom request context
    /// </summary>
    /// <typeparam name="TContext">The type of the request context</typeparam>
    /// <typeparam name="TResponse">The type of the response</typeparam>
    /// <param name="idempotencyKey">The unique idempotency key for the request</param>
    /// <param name="context">The request context</param>
    /// <param name="executionFunction">The function to execute if this is not a duplicate request</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response from either cache or execution</returns>
    Task<TResponse> ProcessWithContextAsync<TContext, TResponse>(
        string idempotencyKey,
        TContext context,
        Func<TContext, CancellationToken, Task<TResponse>> executionFunction,
        CancellationToken cancellationToken = default);
}
