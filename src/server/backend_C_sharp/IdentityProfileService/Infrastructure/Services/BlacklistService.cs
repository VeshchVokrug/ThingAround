using Core.Caching.Service;
using IdentityProfileService.Infrastructure.Services.Abstractions;

namespace IdentityProfileService.Infrastructure.Services;

public class BlacklistService : IBlacklistService
{
    private readonly ICacheService _cache;
    private const string KeyPrefix = "blacklist";
    
    public BlacklistService(ICacheService cache)
    {
        _cache = cache;
    }
    
    public async Task BlacklistTokenAsync(string jti, TimeSpan expiration, CancellationToken ct = default)
    {
        await _cache.SetAsync($"{KeyPrefix}:{jti}", "revoked", expiration, ct);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        return await _cache.ExistsAsync($"{KeyPrefix}:{jti}", ct);
    }
}