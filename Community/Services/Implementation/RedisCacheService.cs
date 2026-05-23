using BreastCancer.Community.Options;
using BreastCancer.Community.Services.Interface;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace BreastCancer.Community.Services.Implementation;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisSettings _redisSettings;
    private readonly ILogger<RedisCacheService> _logger;

    private const string KeyPrefix = "rehla:community:";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisSettings> redisSettings,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _redisSettings = redisSettings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(BuildKey(key));

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            try
            {
                byte[] bytes = value!;
                if (bytes.Length == 0) return default;

                var deserialized = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return deserialized;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Cache deserialization failed for key: {Key}. Value will be ignored. This usually indicates a model schema change.", key);
                return default;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache get operation cancelled for key: {Key}", key);
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis operation failed when reading key: {Key}. Falling through to source.", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving cache value for key: {Key}. Falling through to source.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

        if (value is null)
            throw new ArgumentNullException(nameof(value), "Cache value cannot be null. Use DeleteAsync to remove keys.");

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var expiry = ttl ?? TimeSpan.FromSeconds(_redisSettings.DefaultTTLSeconds);

            byte[] serialized;
            try
            {
                serialized = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize value for cache key: {Key}. Object may have circular references or non-serializable properties.", key);
                return;
            }

            await db.StringSetAsync(BuildKey(key), serialized, expiry);
            _logger.LogDebug("Set cache for key: {Key} with TTL: {TTL}s", key, expiry.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache set operation cancelled for key: {Key}", key);
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis operation failed when writing key: {Key}. Cache miss will occur on next read.", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting cache value for key: {Key}. Cache miss will occur on next read.", key);
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var deleted = await db.KeyDeleteAsync(BuildKey(key));

            _logger.LogDebug(
                deleted ? "Deleted cache key: {Key}" : "Cache key not found for deletion: {Key}",
                key);

            return deleted;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache delete operation cancelled for key: {Key}", key);
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis operation failed when deleting key: {Key}. Key may still exist in cache.", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting cache key: {Key}. Key may still exist in cache.", key);
            return false;
        }
    }

    private static string BuildKey(string key) => $"{KeyPrefix}{key}";
}