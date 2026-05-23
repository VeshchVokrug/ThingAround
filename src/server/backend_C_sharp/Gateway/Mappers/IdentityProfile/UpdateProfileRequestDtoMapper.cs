using Gateway.Models;
using IdentityProfileService.Grpc;
using UpdateProfileRequest = Gateway.Models.UpdateProfileRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class UpdateProfileRequestDtoMapper
{
    public static IdentityProfileService.Grpc.UpdateProfileRequest ToGrpc(this UpdateProfileRequest source)
    {
        var request = new IdentityProfileService.Grpc.UpdateProfileRequest();

        if (source.Name is not null)
        {
            request.Name = source.Name;
        }

        if (source.Bio is not null)
        {
            request.Bio = source.Bio;
        }

        if (source.AvatarUrl is not null)
        {
            request.AvatarUrl = source.AvatarUrl;
        }

        return request;
    }
}

