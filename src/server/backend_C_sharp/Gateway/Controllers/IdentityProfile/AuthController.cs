using Gateway.Mappers.IdentityProfile;
using Gateway.Models;
using IdentityProfileService.Grpc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthRequest = Gateway.Models.AuthRequest;
using AuthResponse = Gateway.Models.AuthResponse;
using RefreshRequest = Gateway.Models.RefreshRequest;

namespace Gateway.Controllers.IdentityProfile;

/// <summary>
/// Методы аутентификации и управления токенами пользователя.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IdentityProfileInternal.IdentityProfileInternalClient _client;
    private const string RefreshTokenCookieName = "refreshToken";
    
    public AuthController(IdentityProfileInternal.IdentityProfileInternalClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Регистрирует нового пользователя по email и паролю.
    /// </summary>
    /// <param name="request">Данные регистрации: email и пароль.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пара access/refresh токенов.</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] AuthRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.RegisterAsync(request.ToGrpc(), cancellationToken: ct);
        var authDto = grpcResponse.ToDto();
        
        SetRefreshTokenCookie(grpcResponse.RefreshToken, grpcResponse.RefreshTokenExpiresHours);
        
        return Ok(authDto);
    }

    /// <summary>
    /// Выполняет вход пользователя и возвращает JWT-токены.
    /// </summary>
    /// <param name="request">Учетные данные пользователя.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пара access/refresh токенов.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.LoginAsync(request.ToGrpc(), cancellationToken: ct);
        var authDto = grpcResponse.ToDto();
        
        SetRefreshTokenCookie(grpcResponse.RefreshToken, grpcResponse.RefreshTokenExpiresHours);
        
        return Ok(authDto); 
    }

    /// <summary>
    /// Обновляет access-токен по паре access/refresh токенов.
    /// </summary>
    /// <param name="request">Текущий access токен.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Новая пара access/refresh токенов.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken);

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ApiErrorResponse(StatusCodes.Status401Unauthorized,"Refresh token is missing"));
        }
        
        var grpcResponse = await _client.RefreshAsync(request.ToGrpc(refreshToken), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        var authDto = grpcResponse.ToDto();

        SetRefreshTokenCookie(grpcResponse.RefreshToken, grpcResponse.RefreshTokenExpiresHours);
        
        return Ok(authDto);
    }

    /// <summary>
    /// Выход пользователя с отзывом refresh-токена.
    /// </summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ при успешном выходе.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken);
        
        var grpcRequest = new IdentityProfileService.Grpc.LogoutRequest { RefreshToken = refreshToken };
        
        await _client.LogoutAsync(grpcRequest, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        
        Response.Cookies.Delete(RefreshTokenCookieName);
        
        return NoContent();
    }
    
    private void SetRefreshTokenCookie(string token, string expiresHoursStr)
    {
        if (string.IsNullOrEmpty(token)) return;
        
        if (!double.TryParse(expiresHoursStr, out var expiresHours))
            expiresHours = 72; 

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(expiresHours),
            Path = "/"
        };

        Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }
}




