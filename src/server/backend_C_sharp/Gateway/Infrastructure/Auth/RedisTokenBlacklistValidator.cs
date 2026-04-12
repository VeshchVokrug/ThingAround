using System.Security.Claims;
using Core.Caching.Service;

namespace Gateway.Infrastructure.Auth;

public sealed class RedisTokenBlacklistValidator : ITokenBlacklistValidator
{
    private readonly ICacheService _cacheService;
    private readonly IRedisPrefixResolver _prefixResolver;

    public RedisTokenBlacklistValidator(
        ICacheService cacheService,
        IRedisPrefixResolver prefixResolver)
    {
        _cacheService = cacheService;
        _prefixResolver = prefixResolver;
    }

    public async Task<bool> ValidateAsync(HttpContext httpContext, ClaimsPrincipal? principal, CancellationToken ct = default)
    {
        if (principal is null)
        {
            return false;
        }

        var jti = principal.FindFirstValue("jti");

        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        if (!_prefixResolver.TryResolvePrefix(httpContext.Request, out var prefix))
        {
            return false;
        }

        var key = BuildBlacklistKey(prefix, jti);

        var exists = await _cacheService.ExistsAsync(key, ct);

        return !exists;
    }

    private static string BuildBlacklistKey(string prefix, string jti)
    {
        var normalizedPrefix = prefix.EndsWith(':') ? prefix : $"{prefix}:";
        return $"{normalizedPrefix}blacklist:{jti}";
    }
}



