using FluentAssertions;
using Idempwanna.Core.Implementations;
using Idempwanna.Core.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Idempwanna.Tests;

public class IdempotencyServiceTests
{
    private readonly IIdempotencyCache _cache;
    private readonly ILogger<DefaultIdempotencyService> _logger;
    private readonly DefaultIdempotencyService _service;

    public IdempotencyServiceTests()
    {
        _cache = Substitute.For<IIdempotencyCache>();
        _logger = Substitute.For<ILogger<DefaultIdempotencyService>>();
        _service = new DefaultIdempotencyService(_cache, _logger);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnCachedResponse_WhenKeyExists()
    {
        // Arrange
        var idempotencyKey = "test-key";
        var expectedResponse = "cached-response";
        _cache.GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, expectedResponse)));

        // Act
        var result = await _service.ProcessAsync<string>(
            idempotencyKey,
            _ => throw new Exception("This should not be called"),
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await _cache.Received(1).GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<TimeSpan?>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldExecuteFunction_WhenKeyDoesNotExist()
    {
        // Arrange
        var idempotencyKey = "test-key";
        var expectedResponse = "new-response";
        _cache.GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(bool, string?)>((false, null)));

        // Act
        var result = await _service.ProcessAsync<string>(
            idempotencyKey,
            _ => Task.FromResult(expectedResponse),
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await _cache.Received(1).GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            idempotencyKey, 
            expectedResponse, 
            null, 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWithContextAsync_ShouldReturnCachedResponse_WhenKeyExists()
    {
        // Arrange
        var idempotencyKey = "test-key";
        var context = new TestContext { Id = 42 };
        var expectedResponse = "cached-response";
        _cache.GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, expectedResponse)));

        // Act
        var result = await _service.ProcessWithContextAsync<TestContext, string>(
            idempotencyKey,
            context,
            (_, _) => throw new Exception("This should not be called"),
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await _cache.Received(1).GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<TimeSpan?>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWithContextAsync_ShouldExecuteFunction_WhenKeyDoesNotExist()
    {
        // Arrange
        var idempotencyKey = "test-key";
        var context = new TestContext { Id = 42 };
        var expectedResponse = "new-response";
        _cache.GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(bool, string?)>((false, null)));

        // Act
        var result = await _service.ProcessWithContextAsync<TestContext, string>(
            idempotencyKey,
            context,
            (ctx, _) => 
            {
                ctx.Id.Should().Be(42);
                return Task.FromResult(expectedResponse);
            },
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await _cache.Received(1).GetAsync<string>(idempotencyKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            idempotencyKey, 
            expectedResponse, 
            null, 
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ProcessAsync_ShouldThrowArgumentException_WhenKeyIsNullOrEmpty(string key)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.ProcessAsync<string>(
                key,
                _ => Task.FromResult("response"),
                CancellationToken.None));
    }

    private class TestContext
    {
        public int Id { get; set; }
    }
}
