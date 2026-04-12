using Grpc.Core;

namespace Gateway.Mappers.IdentityProfile;

public static class AuthorizationMetadataMapper
{
    public static Metadata ToAuthorizationMetadata(this HttpRequest request)
    {
        var metadata = new Metadata();

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

