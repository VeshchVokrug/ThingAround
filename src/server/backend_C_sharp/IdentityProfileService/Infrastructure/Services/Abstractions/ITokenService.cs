using System.Security.Claims;
using IdentityProfileService.Dal;

namespace IdentityProfileService.Infrastructure.Services.Abstractions;

public interface ITokenService
{
    string GenerateJwtToken(Guid id, Role role, string email);
    string GenerateRefreshToken();
    
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}