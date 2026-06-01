using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Caching.Service;
using IdentityProfileService.Exceptions;
using IdentityProfileService.Infrastructure.Services.Abstractions;

namespace IdentityProfileService.Infrastructure.Services;

public class BlacklistService : IBlacklistService
{
    private readonly ICacheService _cache;
    private const string KeyPrefix = "blacklist";
    private readonly TimeProvider _timeProvider;
    
    public BlacklistService(ICacheService cache, TimeProvider timeProvider)
    {
        _cache = cache;
        _timeProvider = timeProvider;
    }
    
    public async Task BlacklistTokenAsync(ClaimsPrincipal userPrincipal, CancellationToken ct = default)
    {
        var jti = userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var exp = userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(exp))
        {
            throw new InvalidTokenException();
        }
        
        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
        var now = _timeProvider.GetUtcNow();
        var remaining = expirationTime - now;
        
        if (remaining > TimeSpan.Zero)
        {
            await _cache.SetAsync($"{KeyPrefix}:{jti}", "revoked", remaining, ct);
        }
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        return await _cache.ExistsAsync($"{KeyPrefix}:{jti}", ct);
    }
}