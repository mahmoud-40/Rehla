namespace BreastCancer.Community.Services.Interface;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class;

    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
}
