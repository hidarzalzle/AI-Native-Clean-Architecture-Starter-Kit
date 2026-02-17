using Application.Abstractions;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer mux) : ICacheService
{
    public async Task<bool> TrySetIdempotencyAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        var db = mux.GetDatabase();
        return await db.StringSetAsync(key, "1", ttl, When.NotExists);
    }
}

public class InMemoryCacheService : ICacheService
{
    private readonly HashSet<string> _keys = [];
    public Task<bool> TrySetIdempotencyAsync(string key, TimeSpan ttl, CancellationToken ct) => Task.FromResult(_keys.Add(key));
}
