using IdentityProfileService.Dto;
using IdentityProfileService.Grpc;

namespace IdentityProfileService.Mapper;

public static class TokenMapper
{
    public static AuthResponse ToGrpc(this AuthTokenDto dto)
    {
        return new AuthResponse
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            RefreshTokenExpiresHours = dto.RefreshTokenExpiresHours
        };
    }
}