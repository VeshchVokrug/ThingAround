using Gateway.Mappers.IdentityProfile;
using Gateway.Models;
using IdentityProfileService.Grpc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthRequest = Gateway.Models.AuthRequest;
using AuthResponse = Gateway.Models.AuthResponse;
using LogoutRequest = Gateway.Models.LogoutRequest;
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

        return Ok(grpcResponse.ToDto());
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

        return Ok(grpcResponse.ToDto()); 
    }

    /// <summary>
    /// Обновляет access-токен по паре access/refresh токенов.
    /// </summary>
    /// <param name="request">Текущие access и refresh токены.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Новая пара access/refresh токенов.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.RefreshAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);

        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Выход пользователя с отзывом refresh-токена.
    /// </summary>
    /// <param name="request">Refresh-токен для отзыва.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Пустой ответ при успешном выходе.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _client.LogoutAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);

        return NoContent();
    }
}




