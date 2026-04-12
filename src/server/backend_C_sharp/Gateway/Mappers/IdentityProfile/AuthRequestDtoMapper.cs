using Gateway.Models;
using IdentityProfileService.Grpc;
using AuthRequest = Gateway.Models.AuthRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class AuthRequestDtoMapper
{
    public static IdentityProfileService.Grpc.AuthRequest ToGrpc(this AuthRequest source)
    {
        return new IdentityProfileService.Grpc.AuthRequest
        {
            Email = source.Email,
            Password = source.Password
        };
    }
}

