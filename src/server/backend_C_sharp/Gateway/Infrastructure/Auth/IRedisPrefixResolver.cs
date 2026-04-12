namespace Gateway.Infrastructure.Auth;

public interface IRedisPrefixResolver
{
    bool TryResolvePrefix(HttpRequest request, out string prefix);
}

