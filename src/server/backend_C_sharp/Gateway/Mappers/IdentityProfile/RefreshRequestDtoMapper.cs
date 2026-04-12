using RefreshRequest = Gateway.Models.RefreshRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class RefreshRequestDtoMapper
{
    public static IdentityProfileService.Grpc.RefreshRequest ToGrpc(this RefreshRequest source)
    {
        return new IdentityProfileService.Grpc.RefreshRequest
        {
            AccessToken = source.AccessToken,
            RefreshToken = source.RefreshToken
        };
    }
}

