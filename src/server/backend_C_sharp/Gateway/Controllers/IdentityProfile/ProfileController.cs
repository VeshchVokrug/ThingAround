using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.S3.Service;
using Gateway.Mappers.IdentityProfile;
using Gateway.Models;
using Google.Protobuf.WellKnownTypes;
using IdentityProfileService.Grpc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CategoriesRequest = Gateway.Models.CategoriesRequest;
using CreateProfileRequest = Gateway.Models.CreateProfileRequest;
using ProfileResponse = Gateway.Models.ProfileResponse;
using UpdateProfileRequest = Gateway.Models.UpdateProfileRequest;
using IdentityProfileClient = IdentityProfileService.Grpc.IdentityProfileService.IdentityProfileServiceClient;

namespace Gateway.Controllers.IdentityProfile;

/// <summary>
/// Методы получения и изменения профиля пользователя.
/// </summary>
[ApiController]
[Route("api/v1/identity/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IdentityProfileClient _client;
    private readonly IS3StorageService _storageService;
    
    public ProfileController(IdentityProfileClient client, IS3StorageService storageService)
    {
        _client = client;
        _storageService = storageService;
    }

    /// <summary>
    /// Возвращает профиль текущего авторизованного пользователя.
    /// </summary>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Персональные данные профиля текущего пользователя.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(CancellationToken ct)
    {
        var grpcResponse = await _client.GetProfileAsync(new Empty(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPersonalDto());
    }

    /// <summary>
    /// Возвращает публичный профиль пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя (GUID).</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Публичные данные профиля указанного пользователя.</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfileById(Guid userId, CancellationToken ct)
    {
        var request = new GetProfileByIdRequest
        {
            Id = userId.ToString()
        };
        var grpcResponse = await _client.GetProfileByIdAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPublicDto());
    }

    /// <summary>
    /// Создает профиль текущего пользователя.
    /// </summary>
    /// <param name="request">Данные для создания профиля.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Созданный профиль с персональными полями.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> CreateProfile([FromBody] CreateProfileRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.CreateProfileAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPersonalDto());
    }

    /// <summary>
    /// Обновляет профиль текущего пользователя.
    /// </summary>
    /// <param name="request">Поля профиля для частичного обновления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Обновленный профиль с персональными полями.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.UpdateProfileAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPersonalDto());
    }

    /// <summary>
    /// Удаляет аватар профиля.
    /// </summary>
    /// <param name="avatarUrl">Ссылка на аватар профиля.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveAvatar([FromQuery] string avatarUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return BadRequest("URL аватара не указан.");
        }
        
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _client.RemoveAvatarAsync(new Empty(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        
        var expectedPrefix = $"users/{userId}/profile/";
        var key = _storageService.GetKeyFromUrl(avatarUrl);

        if (!string.IsNullOrEmpty(key) && key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (await _storageService.FileExistsAsync(key))
            {
                await _storageService.DeleteFileAsync(key);
            }
        }

        return NoContent();
    }
    
    /// <summary>
    /// Добавляет категории в список избранных категорий пользователя.
    /// </summary>
    /// <param name="request">Список категорий для добавления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Профиль с обновленным списком избранных категорий.</returns>
    [HttpPost("favorite-categories")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> AddFavoriteCategories([FromBody] CategoriesRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.AddFavoriteCategoriesAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPersonalDto());
    }
    
    /// <summary>
    /// Удаляет категории из списка избранных категорий пользователя.
    /// </summary>
    /// <param name="request">Список категорий для удаления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Профиль с обновленным списком избранных категорий.</returns>
    [HttpDelete("favorite-categories")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> RemoveFavoriteCategories([FromBody] CategoriesRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.RemoveFavoriteCategoriesAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToPersonalDto());
    }
}

