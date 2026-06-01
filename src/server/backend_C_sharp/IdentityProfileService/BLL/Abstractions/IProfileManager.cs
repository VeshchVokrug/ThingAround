using IdentityProfileService.Dto;

namespace IdentityProfileService.BLL.Abstractions;

public interface IProfileManager
 {
     Task<ProfileDto> GetProfileAsync(Guid id, CancellationToken ct);
     Task<ProfileDto> CreateAsync(ProfileDto dto, CancellationToken ct);
     Task UpdateAsync(ProfileDto dto, CancellationToken ct);
     Task AddFavoriteCategoryAsync(Guid id, List<string> favoriteCategories, CancellationToken ct);
     Task RemoveAvatarAsync(Guid profileId, CancellationToken ct);
     Task RemoveFavoriteCategoryAsync(Guid id, List<string> favoriteCategories, CancellationToken ct);
 }