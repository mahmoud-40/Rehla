using BreastCancer.Community.Options;
using BreastCancer.Community.Services.Interface;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace BreastCancer.Tests.Integration;

[Collection("Redis")]
public class RedisCacheServiceIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private ICacheService? _cacheService;

    public async Task InitializeAsync()
    {
        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Create connection to Redis
        var connectionString = _redisContainer.GetConnectionString();
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(options);

        // Setup cache service
        var redisSettings = new RedisSettings
        {
            ConnectionString = connectionString,
            DefaultTTLSeconds = 3600
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_connectionMultiplexer);

        services.AddSingleton<IOptions<RedisSettings>>(new OptionsWrapper<RedisSettings>(redisSettings));

        services.AddScoped<ICacheService, BreastCancer.Community.Services.Implementation.RedisCacheService>();

        var serviceProvider = services.BuildServiceProvider();
        _cacheService = serviceProvider.GetRequiredService<ICacheService>();
    }

    public async Task DisposeAsync()
    {
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }

        _connectionMultiplexer?.Dispose();
    }

    [Fact]
    public async Task SetAsync_Then_GetAsync_ReturnsValueSuccessfully()
    {
        // Arrange
        var key = "test:key:string";
        var value = "Hello, Redis!";

        // Act
        await _cacheService!.SetAsync(key, value);
        var retrieved = await _cacheService.GetAsync<string>(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "nonexistent:key";

        // Act
        var result = await _cacheService!.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithCustomTTL_ExpiresAfterTTL()
    {
        // Arrange
        var key = "test:ttl:key";
        var value = "Short lived value";
        var ttl = TimeSpan.FromSeconds(2);

        // Act
        await _cacheService!.SetAsync(key, value, ttl);
        var beforeExpiry = await _cacheService.GetAsync<string>(key);

        await Task.Delay(2500); // Wait for expiration
        var afterExpiry = await _cacheService.GetAsync<string>(key);

        // Assert
        beforeExpiry.Should().Be(value);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesValueFromCache()
    {
        // Arrange
        var key = "test:delete:key";
        var value = "Value to delete";
        await _cacheService!.SetAsync(key, value);

        // Act
        var beforeDelete = await _cacheService.GetAsync<string>(key);
        await _cacheService.DeleteAsync(key);
        var afterDelete = await _cacheService.GetAsync<string>(key);

        // Assert
        beforeDelete.Should().Be(value);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_WithComplexObject()
    {
        // Arrange
        var key = "test:complex:object";
        var testObject = new TestPost
        {
            Id = 123,
            Name = "Test Post",
            Content = "This is a test post content",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _cacheService!.SetAsync(key, testObject);
        var retrieved = await _cacheService.GetAsync<TestPost>(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(123);
        retrieved.Name.Should().Be("Test Post");
        retrieved.Content.Should().Be("This is a test post content");
    }

    private class TestPost
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    [Fact]
    public async Task SetAsync_WithDefaultTTL_UsesSettingsDefaultTTL()
    {
        // Arrange
        var key = "test:default:ttl";
        var value = "Test value with default TTL";

        // Act
        await _cacheService!.SetAsync(key, value); // No TTL specified
        var retrieved = await _cacheService.GetAsync<string>(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task MultipleSetAndDelete_WorkCorrectly()
    {
        // Arrange
        var keys = new[] { "key:1", "key:2", "key:3" };
        var values = new[] { "value1", "value2", "value3" };

        // Act & Assert
        for (int i = 0; i < keys.Length; i++)
        {
            await _cacheService!.SetAsync(keys[i], values[i]);
            var retrieved = await _cacheService.GetAsync<string>(keys[i]);
            retrieved.Should().Be(values[i]);

            await _cacheService.DeleteAsync(keys[i]);
            var deleted = await _cacheService.GetAsync<string>(keys[i]);
            deleted.Should().BeNull();
        }
    }
}
