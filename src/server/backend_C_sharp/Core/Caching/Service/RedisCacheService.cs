using System.Text.Json;
using Google.Protobuf;
using StackExchange.Redis;

namespace Core.Caching.Service;

/// <summary>
/// Кеширование в Redis как JSON, так и byte[] объектов.
/// </summary>
internal class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly string _prefix;
    private readonly JsonSerializerOptions _options;

    public RedisCacheService(IConnectionMultiplexer redis, JsonSerializerOptions? options = null, string prefix = "")
    {
        _db = redis.GetDatabase();
        _prefix = string.IsNullOrWhiteSpace(prefix) ? "" : $"{prefix}:";
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public string GetFullKey(string key) => $"{_prefix}{key}";

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, _options);
        await _db.StringSetAsync(GetFullKey(key), json, (Expiration)expiration!);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(GetFullKey(key))
            .WaitAsync(ct);
        return value.IsNullOrEmpty
            ? default 
            : JsonSerializer.Deserialize<T>((string)value!, _options);
    }

    public async Task SetProtoAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : IMessage<T>
    {
        var data = value.ToByteArray();
        await _db.StringSetAsync(GetFullKey(key), data, (Expiration)expiration!)
            .WaitAsync(ct);
    }

    public async Task<T?> GetProtoAsync<T>(string key, MessageParser<T> parser, CancellationToken ct = default) where T : IMessage<T>
    {
        var data = await _db.StringGetAsync(GetFullKey(key))
            .WaitAsync(ct);
        return data.IsNullOrEmpty 
            ? default 
            : parser.ParseFrom((byte[])data!);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) => await _db.KeyExistsAsync(GetFullKey(key)).WaitAsync(ct);

    public async Task RemoveAsync(string key, CancellationToken ct = default) => await _db.KeyDeleteAsync(GetFullKey(key)).WaitAsync(ct);
}