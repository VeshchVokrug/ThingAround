namespace IdentityProfileService.Infrastructure.Services.Abstractions;

public interface IBlacklistService
{
    Task BlacklistTokenAsync(string jti, TimeSpan expiration, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);
}