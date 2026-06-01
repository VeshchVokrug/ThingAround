using Grpc.Core;
using System.Security.Claims;

namespace Gateway.Mappers.IdentityProfile;

public static class AuthorizationMetadataMapper
{
    public static Metadata ToAuthorizationMetadata(this HttpRequest request)
    {
        var metadata = new Metadata();

        var user = request.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                metadata.Add("x-user-id", userId);
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                metadata.Add("x-user-role", role);
            }
        }

        if (!request.Headers.TryGetValue("Authorization", out var values))
        {
            return metadata;
        }

        var token = values.ToString();
        if (!string.IsNullOrWhiteSpace(token))
        {
            metadata.Add("authorization", token);
        }

        return metadata;
    }
}
