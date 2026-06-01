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
            Bio = source.Bio
        };

        if (source.AvatarUrl != null)
        {
            request.AvatarUrl = source.AvatarUrl;
        }

        if (source.PhoneNumber != null)
        {
            request.PhoneNumber = source.PhoneNumber;
        }
        
        request.FavoriteCategories.AddRange(source.FavoriteCategories);
        return request;
    }
}