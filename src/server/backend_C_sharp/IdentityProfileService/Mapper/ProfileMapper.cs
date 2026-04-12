using IdentityProfileService.Dal;
using IdentityProfileService.Dto;
using IdentityProfileService.Grpc;

namespace IdentityProfileService.Mapper;

public static class ProfileMapper
{
    public static ProfileDto ToDto(this Profile profile)
    {
        return new ProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            Reputation = profile.Reputation,
            FavoriteCategories = profile.FavoriteCategories,
        };
    }

    public static Profile ToEntity(this ProfileDto dto)
    {
        return new Profile
        {
            Id = dto.Id,
            Name = dto.Name ?? "",
            Bio = dto.Bio ?? "",
            AvatarUrl = dto.AvatarUrl,
            Reputation = dto.Reputation,
            FavoriteCategories = dto.FavoriteCategories,
        };
    }

    public static PersonalProfileResponse ToPersonalGrpc(this ProfileDto dto)
    {
        var response = new PersonalProfileResponse
        {
            Id = dto.Id.ToString(),
            Reputation = (double)(dto.Reputation ?? 0)
        };
        
        if (dto.Name != null) response.Name = dto.Name;
        if (dto.Bio != null) response.Bio = dto.Bio;
        if (dto.AvatarUrl != null) response.AvatarUrl = dto.AvatarUrl;

        if (dto.FavoriteCategories != null && dto.FavoriteCategories.Count != 0)
        {
            response.FavoriteCategories.AddRange(dto.FavoriteCategories);
        }

        return response;
    }

    public static PublicProfileResponse ToPublicGrpc(this ProfileDto dto)
    {
        var response = new PublicProfileResponse
        {
            Id = dto.Id.ToString(),
            Reputation = (double)(dto.Reputation ?? 0)
        };
        
        if (dto.Name != null) response.Name = dto.Name;
        if (dto.Bio != null) response.Bio = dto.Bio;
        if (dto.AvatarUrl != null) response.AvatarUrl = dto.AvatarUrl;

        return response;
    }

    public static Presentation.Models.CreateProfileRequest ToDto(this CreateProfileRequest request)
    {
        return new Presentation.Models.CreateProfileRequest(
            Name: request.Name, 
            Bio: request.Bio,
            FavoriteCategories: request.FavoriteCategories.Count != 0
                ? request.FavoriteCategories.ToList() 
                : null
        );
    }

    public static ProfileDto ToDto(this UpdateProfileRequest request)
    {
        return new ProfileDto
        {
            Name = request.HasName ? request.Name : null,
            Bio = request.HasBio ? request.Bio : null
        };
    }
}