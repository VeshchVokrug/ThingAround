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

namespace Gateway.Controllers.IdentityProfile;

/// <summary>
/// Методы получения и изменения профиля пользователя.
/// </summary>
[ApiController]
[Route("api/v1/identity/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IdentityProfileInternal.IdentityProfileInternalClient _client;

    public ProfileController(IdentityProfileInternal.IdentityProfileInternalClient client)
    {
        _client = client;
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

