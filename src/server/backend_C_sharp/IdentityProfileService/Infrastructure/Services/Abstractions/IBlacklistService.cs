using System.Security.Claims;

namespace IdentityProfileService.Infrastructure.Services.Abstractions;

public interface IBlacklistService
{
    Task BlacklistTokenAsync(ClaimsPrincipal userPrincipal, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);
}