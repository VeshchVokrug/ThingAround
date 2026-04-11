using IdentityProfileService.Dal;

namespace IdentityProfileService.Infrastructure.Repository.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByUserIdAsync(Guid id, CancellationToken ct);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task<RefreshToken?> GetByTokenAndUserIdAsync(Guid userId, string token, CancellationToken ct);
    Task<List<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct);
    Task DeleteByIdAsync(Guid id, CancellationToken ct);
    Task DeleteRangeAsync(IEnumerable<RefreshToken> tokens, CancellationToken ct);
}