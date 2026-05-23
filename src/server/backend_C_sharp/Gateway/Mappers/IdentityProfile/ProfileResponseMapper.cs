using IdentityProfileService.Grpc;
using ProfileResponse = Gateway.Models.ProfileResponse;

namespace Gateway.Mappers.IdentityProfile;

public static class ProfileResponseMapper
{
    public static ProfileResponse ToPersonalDto(this PersonalProfileResponse source)
    {
        return new ProfileResponse
        {
            Id = source.Id,
            Name = source.HasName ? source.Name : null,
            Bio = source.HasBio ? source.Bio : null,
            AvatarUrl = source.HasAvatarUrl ? source.AvatarUrl : null,
            Reputation = source.Reputation,
            FavoriteCategories = source.FavoriteCategories.ToList(),
            PhoneNumber = source.PhoneNumber
        };
    }

    public static ProfileResponse ToPublicDto(this PublicProfileResponse source)
    {
        return new ProfileResponse
        {
            Id = source.Id,
            Name = source.HasName ? source.Name : null,
            Bio = source.HasBio ? source.Bio : null,
            AvatarUrl = source.HasAvatarUrl ? source.AvatarUrl : null,
            Reputation = source.Reputation,
            PhoneNumber = source.PhoneNumber
        };
    }
}


