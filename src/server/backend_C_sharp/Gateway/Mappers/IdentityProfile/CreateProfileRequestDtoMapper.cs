using Gateway.Models;
using IdentityProfileService.Grpc;
using CreateProfileRequest = Gateway.Models.CreateProfileRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class CreateProfileRequestDtoMapper
{
    public static IdentityProfileService.Grpc.CreateProfileRequest ToGrpc(this CreateProfileRequest source)
    {
        var request = new IdentityProfileService.Grpc.CreateProfileRequest
        {
            Name = source.Name,
            Bio = source.Bio,
            AvatarUrl = source.AvatarUrl,
        };

        request.FavoriteCategories.AddRange(source.FavoriteCategories);
        return request;
    }
}