using IdentityProfileService.Dal;
using IdentityProfileService.Infrastructure.Persistence;
using IdentityProfileService.Infrastructure.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IdentityProfileService.Infrastructure.Repository;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Token == token, ct);
    }

    public async Task<RefreshToken?> GetByTokenAndUserIdAsync(Guid userId, string token, CancellationToken ct)
    {
        return await _context.RefreshTokens
            .AsNoTracking() 
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Token == token, ct);
    }

    public async Task<List<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken ct)
    {
        await _context.RefreshTokens
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteRangeAsync(IEnumerable<RefreshToken> tokens, CancellationToken ct)
    {
        _context.RefreshTokens.RemoveRange(tokens);
        await _context.SaveChangesAsync(ct);
    }
}