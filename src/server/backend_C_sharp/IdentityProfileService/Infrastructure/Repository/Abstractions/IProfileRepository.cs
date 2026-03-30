using System.Security.Claims;
using IdentityProfileService.Dal;
using IdentityProfileService.Dto;

namespace IdentityProfileService.Infrastructure.Repository.Abstractions;

public interface IProfileRepository
{
    Task<Profile> AddAsync(Profile profile, CancellationToken ct);
    Task<Profile?> FindByAccountIdAsync(Guid id, CancellationToken ct);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct);
    Task<bool> UpdateWithoutReputationAsync(ProfileDto profile, CancellationToken ct);
    Task UpdateFavoriteCategoriesAsync(Guid profileId, IEnumerable<string> favoriteCategories, CancellationToken ct);
}