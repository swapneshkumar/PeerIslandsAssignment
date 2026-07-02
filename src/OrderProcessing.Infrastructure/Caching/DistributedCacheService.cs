using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using OrderProcessing.Application.Abstractions;

namespace OrderProcessing.Infrastructure.Caching;

public sealed class DistributedCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(key, cancellationToken);
        return value is null ? default : JsonSerializer.Deserialize<T>(value, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, JsonOptions);
        return cache.SetStringAsync(key, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(key, cancellationToken);
}
