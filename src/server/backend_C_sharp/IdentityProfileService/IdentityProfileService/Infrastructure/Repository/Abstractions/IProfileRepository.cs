using System.Security.Claims;
using IdentityProfileService.Dal;

namespace IdentityProfileService.Infrastructure.Repository.Abstractions;

public interface IProfileRepository
{
    Task AddAsync(Profile profile, CancellationToken ct);
    Task<Profile?> FindByAccountIdAsync(Guid id, CancellationToken ct);
    Task DeleteByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(Profile profile, CancellationToken ct);
}