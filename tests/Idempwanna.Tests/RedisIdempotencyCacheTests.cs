using FluentAssertions;
using Idempwanna.Core.Implementations;
using StackExchange.Redis;
using System.Text.Json;
using Testcontainers.Redis;
using Xunit;

namespace Idempwanna.Tests;

/// <summary>
/// Tests for the Redis implementation of the idempotency cache
/// </summary>
public class RedisIdempotencyCacheTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly TimeSpan _defaultExpirationTime = TimeSpan.FromMinutes(5);
    private RedisIdempotencyCache _cache;
    private IConnectionMultiplexer _redis;

    /// <summary>
    /// Sets up a real Redis instance using Testcontainers for integration testing
    /// </summary>
    public RedisIdempotencyCacheTests()
    {
        // Start a Redis container for testing
        _redisContainer = new RedisBuilder()
            .WithImage("redis:alpine")
            .WithCleanUp(true)
            .Build();

        // Cache and Redis connection will be initialized in InitializeAsync
        _redis = null!;
        _cache = null!;
    }

    /// <summary>
    /// Initialize the Redis container and connection before tests
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start the Redis container
        await _redisContainer.StartAsync();

        // Connect to Redis
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        // Initialize the cache with the Redis connection
        _cache = new RedisIdempotencyCache(_redis, _defaultExpirationTime);
    }

    /// <summary>
    /// Clean up resources after tests
    /// </summary>
    public async Task DisposeAsync()
    {
        // Clean up the cache
        if (_cache != null)
        {
            _cache.Dispose();
        }

        // Clean up the Redis connection
        if (_redis != null)
        {
            await _redis.CloseAsync();
            _redis.Dispose();
        }

        // Stop and remove the Redis container
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        // Arrange
        var idempotencyKey = "non-existent-key";

        // Act
        var result = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Assert
        result.Found.Should().BeFalse();
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var idempotencyKey = "test-key-1";
        var expectedResponse = new TestResponse
        {
            Id = 123,
            Name = "Test Response",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _cache.SetAsync(idempotencyKey, expectedResponse);
        var result = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Assert
        result.Found.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Id.Should().Be(expectedResponse.Id);
        result.Response!.Name.Should().Be(expectedResponse.Name);
        result.Response!.CreatedAt.Should().BeCloseTo(expectedResponse.CreatedAt, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task SetAsync_ShouldOverwriteExistingKey()
    {
        // Arrange
        var idempotencyKey = "test-key-2";
        var originalResponse = new TestResponse { Id = 100, Name = "Original" };
        var updatedResponse = new TestResponse { Id = 200, Name = "Updated" };

        // Act
        await _cache.SetAsync(idempotencyKey, originalResponse);
        await _cache.SetAsync(idempotencyKey, updatedResponse);
        var result = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Assert
        result.Found.Should().BeTrue();
        result.Response!.Id.Should().Be(updatedResponse.Id);
        result.Response!.Name.Should().Be(updatedResponse.Name);
    }

    [Fact]
    public async Task SetAsync_ShouldRespectCustomExpirationTime()
    {
        // Arrange
        var idempotencyKey = "expiring-key";
        var response = new TestResponse { Id = 300, Name = "Expiring Test" };
        var shortExpiration = TimeSpan.FromMilliseconds(500); // Very short expiration for testing

        // Act
        await _cache.SetAsync(idempotencyKey, response, shortExpiration);

        // Get the result immediately - should exist
        var immediateResult = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Wait for the key to expire
        await Task.Delay(1000);

        // Get the result after expiration - should not exist
        var afterExpirationResult = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Assert
        immediateResult.Found.Should().BeTrue();
        immediateResult.Response!.Id.Should().Be(response.Id);

        afterExpirationResult.Found.Should().BeFalse();
        afterExpirationResult.Response.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldUseDefaultExpirationTime()
    {
        // This test verifies the default expiration is used but doesn't wait for it
        // since that would make the test too slow

        // Arrange
        var idempotencyKey = "default-expiration-key";
        var response = new TestResponse { Id = 400, Name = "Default Expiration Test" };
        var db = _redis.GetDatabase();

        // Act
        await _cache.SetAsync(idempotencyKey, response); // No expiration specified

        // Check the TTL using Redis commands directly
        var ttl = await db.KeyTimeToLiveAsync($"idempotency:{idempotencyKey}");

        // Assert
        ttl.HasValue.Should().BeTrue();
        ttl!.Value.Should().BeCloseTo(_defaultExpirationTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowException_WhenKeyIsNullOrEmpty()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cache.GetAsync<TestResponse>(null!));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cache.GetAsync<TestResponse>(string.Empty));
    }

    [Fact]
    public async Task SetAsync_ShouldThrowException_WhenKeyIsNullOrEmpty()
    {
        // Arrange
        var response = new TestResponse { Id = 500, Name = "Invalid Key Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cache.SetAsync(null!, response));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cache.SetAsync(string.Empty, response));
    }

    [Fact]
    public async Task GetAsync_WithInvalidJson_ShouldReturnNotFound()
    {
        // Arrange
        var idempotencyKey = "invalid-json-key";
        var db = _redis.GetDatabase();

        // Store invalid JSON directly in Redis
        await db.StringSetAsync($"idempotency:{idempotencyKey}", "{ this is not valid json }", _defaultExpirationTime);

        // Act
        var result = await _cache.GetAsync<TestResponse>(idempotencyKey);

        // Assert
        result.Found.Should().BeFalse();
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task Constructor_WithConnectionString_ShouldCreateValidCache()
    {
        // Arrange
        var connectionString = _redisContainer.GetConnectionString();

        // Act
        using var cacheFromConnectionString = new RedisIdempotencyCache(connectionString);

        // Test basic functionality
        var testKey = "connection-string-test";
        var testResponse = new TestResponse { Id = 600, Name = "Connection String Test" };

        await cacheFromConnectionString.SetAsync(testKey, testResponse);
        var result = await cacheFromConnectionString.GetAsync<TestResponse>(testKey);

        // Assert
        result.Found.Should().BeTrue();
        result.Response!.Id.Should().Be(testResponse.Id);
        result.Response!.Name.Should().Be(testResponse.Name);
    }

    /// <summary>
    /// Test class for JSON serialization/deserialization
    /// </summary>
    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}