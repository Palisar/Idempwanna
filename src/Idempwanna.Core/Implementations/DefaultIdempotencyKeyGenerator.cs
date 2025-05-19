using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Idempwanna.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Idempwanna.Core.Implementations;

/// <summary>
/// Default implementation of the idempotency key generator
/// </summary>
public class DefaultIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    /// <inheritdoc />
    public string GenerateKey(object data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        // Serialize the data to JSON
        var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Compute SHA256 hash of the JSON
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonData));
        
        // Convert to Base64 string and remove any non-alphanumeric characters
        return Convert.ToBase64String(hashBytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");
    }

    /// <inheritdoc />
    public async Task<string> ExtractFromHttpRequestAsync(HttpRequest httpRequest, string headerName = "x-Idempotency-Key")
    {
        if (httpRequest == null)
        {
            throw new ArgumentNullException(nameof(httpRequest));
        }

        // First check if the key is in the headers
        if (httpRequest.Headers.TryGetValue(headerName, out var headerValues) && 
            headerValues.Count > 0 && 
            !string.IsNullOrEmpty(headerValues[0]))
        {
            return headerValues[0];
        }

        // Then check if it's in the query string
        if (httpRequest.Query.TryGetValue(headerName, out var queryValues) && 
            queryValues.Count > 0 && 
            !string.IsNullOrEmpty(queryValues[0]))
        {
            return queryValues[0];
        }

        // If not found in headers or query, try to generate from request body
        // Warning: This requires a request with a seek-able body stream
        if (httpRequest.Body.CanSeek)
        {
            var originalPosition = httpRequest.Body.Position;
            
            try
            {
                httpRequest.Body.Position = 0;
                
                using var reader = new StreamReader(
                    httpRequest.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                
                var bodyContent = await reader.ReadToEndAsync();
                
                // Generate a key from the body content if available
                if (!string.IsNullOrEmpty(bodyContent))
                {
                    return GenerateKey(bodyContent);
                }
            }
            finally
            {
                // Restore the original position
                httpRequest.Body.Position = originalPosition;
            }
        }

        // If no key found, throw an exception
        throw new InvalidOperationException(
            $"No idempotency key found in the request. Please provide a '{headerName}' header, " +
            $"query parameter, or a request body.");
    }

    /// <inheritdoc />
    public string GetKeyFromParameter(object parameterValue)
    {
        if (parameterValue == null)
        {
            throw new ArgumentNullException(nameof(parameterValue));
        }
        
        // If the parameter is a string or primitive type, use it directly
        if (parameterValue is string || parameterValue.GetType().IsPrimitive || parameterValue is Guid)
        {
            return parameterValue.ToString()!;
        }
        
        // Otherwise, generate a key based on the object
        return GenerateKey(parameterValue);
    }
}
