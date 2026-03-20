using IdentityProfileService.Dal;
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

    public async Task AddAsync(Profile profile, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Profile?> FindByAccountIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken ct)
    {
        var existing = await _dbContext.Profiles
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (existing != null)
        {
            _dbContext.Profiles.Remove(existing);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Profile profile, CancellationToken ct)
    {
        var existing = await _dbContext.Profiles
            .FirstOrDefaultAsync(m => m.Id == profile.Id, ct);
        
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(profile);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}