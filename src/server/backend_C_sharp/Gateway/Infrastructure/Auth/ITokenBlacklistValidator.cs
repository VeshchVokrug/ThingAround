using System.Security.Claims;

namespace Gateway.Infrastructure.Auth;

public interface ITokenBlacklistValidator
{
    Task<bool> ValidateAsync(HttpContext httpContext, ClaimsPrincipal? principal, CancellationToken ct = default);
}

