using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Idempwanna.Core.Interfaces;

/// <summary>
/// Generator for idempotency keys
/// </summary>
public interface IIdempotencyKeyGenerator
{
    /// <summary>
    /// Generates an idempotency key from the provided data
    /// </summary>
    /// <param name="data">Data to use for generating the key</param>
    /// <returns>A unique idempotency key</returns>
    string GenerateKey(object data);

    /// <summary>
    /// Extracts an idempotency key from an HTTP request
    /// </summary>
    /// <param name="httpRequest">The HTTP request to extract the key from</param>
    /// <param name="headerName">Optional header name to look for the key</param>
    /// <returns>The extracted idempotency key</returns>
    Task<string> ExtractFromHttpRequestAsync(HttpRequest httpRequest, string headerName = "x-Idempotency-Key");
}
