using LogoutRequest = Gateway.Models.LogoutRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class LogoutRequestDtoMapper
{
    public static IdentityProfileService.Grpc.LogoutRequest ToGrpc(this LogoutRequest source)
    {
        return new IdentityProfileService.Grpc.LogoutRequest
        {
            RefreshToken = source.RefreshToken
        };
    }
}

