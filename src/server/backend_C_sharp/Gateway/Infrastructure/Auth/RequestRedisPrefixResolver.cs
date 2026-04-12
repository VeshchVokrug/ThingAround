using Core.Caching;

namespace Gateway.Infrastructure.Auth;

public sealed class RequestRedisPrefixResolver : IRedisPrefixResolver
{
    private static readonly Dictionary<string, string> PrefixMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["identity"] = RedisKeyPrefixes.IdentityProfilePrefix,
        ["catalog"] = RedisKeyPrefixes.CatalogPrefix
    };

    public bool TryResolvePrefix(HttpRequest request, out string prefix)
    {
        prefix = string.Empty;

        var path = request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            if (PrefixMap.TryGetValue(segment, out var mappedPrefix))
            {
                prefix = mappedPrefix;
                return true;
            }
        }

        return false;
    }
}


