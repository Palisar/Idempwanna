using FluentAssertions;
using Idempwanna.Core.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Idempwanna.Tests;

public class IdempotencyKeyGeneratorTests
{
    private readonly DefaultIdempotencyKeyGenerator _keyGenerator;

    public IdempotencyKeyGeneratorTests()
    {
        _keyGenerator = new DefaultIdempotencyKeyGenerator();
    }

    [Fact]
    public void GenerateKey_ShouldGenerateConsistentHash_ForSameInput()
    {
        // Arrange
        var data1 = new { Id = 123, Name = "Test" };
        var data2 = new { Id = 123, Name = "Test" };
        var data3 = new { Id = 456, Name = "Different" };

        // Act
        var key1 = _keyGenerator.GenerateKey(data1);
        var key2 = _keyGenerator.GenerateKey(data2);
        var key3 = _keyGenerator.GenerateKey(data3);

        // Assert
        key1.Should().NotBeNullOrEmpty();
        key1.Should().Be(key2); // Same data should produce same key
        key1.Should().NotBe(key3); // Different data should produce different key
    }

    [Fact]
    public async Task ExtractFromHttpRequestAsync_ShouldExtractFromHeader()
    {
        // Arrange
        var expectedKey = "test-key-123";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-idempotency-key"] = expectedKey;

        // Act
        var key = await _keyGenerator.ExtractFromHttpRequestAsync(httpContext.Request);

        // Assert
        key.Should().Be(expectedKey);
    }

    [Fact]
    public async Task ExtractFromHttpRequestAsync_ShouldExtractFromCustomHeader()
    {
        // Arrange
        var expectedKey = "test-key-123";
        var customHeaderName = "Custom-Idempotency-Key";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[customHeaderName] = expectedKey;

        // Act
        var key = await _keyGenerator.ExtractFromHttpRequestAsync(httpContext.Request, customHeaderName);

        // Assert
        key.Should().Be(expectedKey);
    }

    [Fact]
    public async Task ExtractFromHttpRequestAsync_ShouldExtractFromQueryString()
    {
        // Arrange
        var expectedKey = "test-key-123";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?x-idempotency-key={expectedKey}");

        // Act
        var key = await _keyGenerator.ExtractFromHttpRequestAsync(httpContext.Request);

        // Assert
        key.Should().Be(expectedKey);
    }

    [Fact]
    public async Task ExtractFromHttpRequestAsync_ShouldGenerateFromBody_WhenNoKeyInHeaderOrQuery()
    {
        // Arrange
        var bodyContent = "{\"id\":123,\"name\":\"Test\"}";
        var expectedKey = _keyGenerator.GenerateKey(bodyContent);
        
        var httpContext = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(bodyContent);
        var stream = new MemoryStream(bodyBytes);
        stream.Position = 0;
        
        httpContext.Request.Body = stream;
        httpContext.Request.ContentLength = bodyBytes.Length;

        // Act
        var key = await _keyGenerator.ExtractFromHttpRequestAsync(httpContext.Request);

        // Assert
        key.Should().Be(expectedKey);
    }

    [Fact]
    public void GetKeyFromParameter_ShouldReturnParameterToString_WhenPrimitiveOrGuid()
    {
        // Arrange
        var guidValue = Guid.NewGuid();
        var intValue = 42;
        var stringValue = "test-key-123";

        // Act
        var guidKey = _keyGenerator.GetKeyFromParameter(guidValue);
        var intKey = _keyGenerator.GetKeyFromParameter(intValue);
        var stringKey = _keyGenerator.GetKeyFromParameter(stringValue);

        // Assert
        guidKey.Should().Be(guidValue.ToString());
        intKey.Should().Be(intValue.ToString());
        stringKey.Should().Be(stringValue);
    }

    [Fact]
    public void GetKeyFromParameter_ShouldGenerateHashBasedKey_WhenComplexObject()
    {
        // Arrange
        var complexObject = new { Id = 123, Name = "Test" };
        var expectedKey = _keyGenerator.GenerateKey(complexObject);

        // Act
        var key = _keyGenerator.GetKeyFromParameter(complexObject);

        // Assert
        key.Should().Be(expectedKey);
    }

    [Fact]
    public void GetKeyFromParameter_ShouldThrowArgumentNullException_WhenParameterIsNull()
    {
        // Act & Assert
        Action act = () => _keyGenerator.GetKeyFromParameter(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
