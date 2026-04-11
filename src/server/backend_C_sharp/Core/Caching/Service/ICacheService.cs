using Google.Protobuf;

namespace Core.Caching.Service;

public interface ICacheService
{
    string GetFullKey(string key);
    
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    
    Task SetProtoAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : IMessage<T>;
    Task<T?> GetProtoAsync<T>(string key, MessageParser<T> parser, CancellationToken ct = default) where T : IMessage<T>;
    
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}