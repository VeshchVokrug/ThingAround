using Gateway.Models;
using IdentityProfileService.Grpc;
using AuthResponse = Gateway.Models.AuthResponse;

namespace Gateway.Mappers.IdentityProfile;

public static class AuthResponseMapper
{
    public static AuthResponse ToDto(this IdentityProfileService.Grpc.AuthResponse source)
    {
        return new AuthResponse
        {
            AccessToken = source.AccessToken,
            RefreshToken = source.RefreshToken,
            RefreshTokenExpiresHours = source.RefreshTokenExpiresHours
        };
    }
}


