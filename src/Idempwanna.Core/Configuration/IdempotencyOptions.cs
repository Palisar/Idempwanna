namespace Idempwanna.Core.Configuration;

/// <summary>
/// Options for configuring the idempotency service
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// Gets or sets the default cache expiration time
    /// </summary>
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the default header name for idempotency keys
    /// </summary>
    public string DefaultHeaderName { get; set; } = "x-idempotency-key";

    /// <summary>
    /// Gets or sets whether to throw an exception when no idempotency key is found
    /// </summary>
    public bool ThrowOnMissingKey { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow extracting idempotency keys from query parameters
    /// </summary>
    public bool AllowQueryParameterKeys { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to allow generating idempotency keys from request body
    /// </summary>
    public bool AllowBodyBasedKeys { get; set; } = false;
}