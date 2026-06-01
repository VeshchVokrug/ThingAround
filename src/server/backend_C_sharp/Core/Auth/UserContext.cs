using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Core.Auth;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public Guid UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
        }
    }

    public string Role => User?.FindFirst(ClaimTypes.Role)?.Value!;

    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}