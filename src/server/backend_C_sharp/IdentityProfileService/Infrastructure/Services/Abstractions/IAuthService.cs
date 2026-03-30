using System.Security.Claims;
using IdentityProfileService.Dto;

namespace IdentityProfileService.Infrastructure.Services.Abstractions;

public interface IAuthService
{
    Task<AuthTokenDto> RegisterAsync(string email, string password, CancellationToken ct);
    Task<AuthTokenDto> LoginAsync(string email, string password, CancellationToken ct);
    Task<AuthTokenDto> RefreshAsync(string accessToken, string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, ClaimsPrincipal userPrincipal, CancellationToken ct);
}