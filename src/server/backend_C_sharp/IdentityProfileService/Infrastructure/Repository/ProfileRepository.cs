using IdentityProfileService.Dal;
using IdentityProfileService.Dto;
using IdentityProfileService.Infrastructure.Persistence;
using IdentityProfileService.Infrastructure.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IdentityProfileService.Infrastructure.Repository;

public class ProfileRepository : IProfileRepository
{
    private readonly AppDbContext _dbContext;

    public ProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Profile> AddAsync(Profile profile, CancellationToken ct)
    {
        if (profile.Id == Guid.Empty) 
            profile.Id = Guid.NewGuid();
        
        await _dbContext.Profiles.AddAsync(profile, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        return profile;
    }

    public async Task<Profile?> FindByAccountIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct)
    {
        var deletedRows = await _dbContext.Profiles
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);
        
        return deletedRows > 0;
    }

    public async Task<bool> UpdateWithoutReputationAsync(ProfileDto profile, CancellationToken ct)
    {
        var query = _dbContext.Profiles.Where(p => p.Id == profile.Id);

        var updatedRows = await query.ExecuteUpdateAsync(s => s
            .SetProperty(p => p.Name, p => profile.Name ?? p.Name)
            .SetProperty(p => p.Bio, p => profile.Bio ?? p.Bio)
            .SetProperty(p => p.AvatarUrl, p => profile.AvatarUrl ?? p.AvatarUrl)
            .SetProperty(p => p.FavoriteCategories, p => profile.FavoriteCategories ?? p.FavoriteCategories),
            cancellationToken: ct);

        return updatedRows > 0;
    }

    public async Task UpdateFavoriteCategoriesAsync(Guid profileId, IEnumerable<string> favoriteCategories, CancellationToken ct)
    {
        await _dbContext.Profiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.FavoriteCategories, favoriteCategories), ct);
    }
}