namespace BreastCancer.Community.Services.Interface;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class;

    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    Task IncrementHashFieldAsync(string key, string field, long incrementBy = 1, CancellationToken cancellationToken = default);

    Task<Dictionary<string, long>> GetHashAllFieldsAsync(string key, CancellationToken cancellationToken = default);

    Task DecrementHashFieldAsync(string key, string field, long decrementBy = 1, CancellationToken cancellationToken = default);

    Task RemoveFromSortedSetAsync(string key, string[] members, CancellationToken cancellationToken = default);
}
