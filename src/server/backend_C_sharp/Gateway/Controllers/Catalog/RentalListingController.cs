using Gateway.Mappers.Catalog;
using Gateway.Mappers.IdentityProfile;
using Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CatalogClient = CatalogService.Grpc.CatalogService.CatalogServiceClient;
using CreateRentalListingRequest = Gateway.Models.CreateRentalListingRequest;
using CreateRentalListingResponse = Gateway.Models.CreateRentalListingResponse;
using PagedRentalListingCardResponse = Gateway.Models.PagedRentalListingCardResponse;
using RentalFilterRequest = Gateway.Models.RentalFilterRequest;
using RentalListing = Gateway.Models.RentalListing;
using RentalListingCardsResponse = Gateway.Models.RentalListingCardsResponse;
using ReservationSlotsRequest = Gateway.Models.ReservationSlotsRequest;
using TryReserveSlotsResponse = Gateway.Models.TryReserveSlotsResponse;

namespace Gateway.Controllers.Catalog;

/// <summary>
/// Методы работы с объявлениями аренды.
/// </summary>
[ApiController]
[Route("api/v1/catalog/rentals")]
[Authorize]
public class RentalListingController : ControllerBase
{
    private readonly CatalogClient _client;

    public RentalListingController(CatalogClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Возвращает объявление по идентификатору.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Полная информация по объявлению.</returns>
    [AllowAnonymous]
    [HttpGet("{listingId}")]
    [ProducesResponseType(typeof(RentalListing), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RentalListing>> GetById(string listingId, CancellationToken ct)
    {
        var request = new CatalogService.Grpc.GetRentalListingRequest
        {
            ListingId = listingId
        };

        var grpcResponse = await _client.GetRentalListingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Возвращает постраничный список объявлений по фильтру.
    /// </summary>
    /// <param name="request">Параметры фильтра и пагинации.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список карточек объявлений.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedRentalListingCardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedRentalListingCardResponse>> GetList([FromQuery] RentalFilterRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.GetRentalListingsAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Возвращает список объявлений пользователя.
    /// </summary>
    /// <param name="ownerId">Идентификатор владельца.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Список карточек объявлений.</returns>
    [HttpGet("by-user/{ownerId}")]
    [ProducesResponseType(typeof(RentalListingCardsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RentalListingCardsResponse>> GetByUser(string ownerId, CancellationToken ct)
    {
        var request = new CatalogService.Grpc.GetRentalListingsByUserRequest
        {
            OwnerId = ownerId
        };

        var grpcResponse = await _client.GetRentalListingsByUserAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Создает новое объявление.
    /// </summary>
    /// <param name="request">Данные объявления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Идентификатор созданного объявления.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRentalListingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateRentalListingResponse>> Create([FromBody] CreateRentalListingRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.CreateRentalListingAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Обновляет объявление.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="request">Полная модель объявления.
    /// Модель состоит из двух частей: метаданные самого объявления и список слотов доступности.
    /// Для обновления метаданных список слотов доступности может отсутствовать в запросе.
    /// Для обновления слотов доступности важно отпровлять полный снапшот объявления со всеми слотами доступности(60 шт с текущей даты).</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPut("{listingId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(string listingId, [FromBody] RentalListing request, CancellationToken ct)
    {
        await _client.UpdateRentalListingAsync(request.ToGrpc(listingId), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return NoContent();
    }

    /// <summary>
    /// Удаляет объявление.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpDelete("{listingId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Remove(string listingId, CancellationToken ct)
    {
        var request = new CatalogService.Grpc.GetRentalListingRequest
        {
            ListingId = listingId
        };

        await _client.RemoveRentalListingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return NoContent();
    }

    /// <summary>
    /// Деактивирует объявление владельцем.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{listingId}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Deactivate(string listingId, CancellationToken ct)
    {
        var request = new CatalogService.Grpc.GetRentalListingRequest
        {
            ListingId = listingId
        };

        await _client.DeactivateRentalListingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return NoContent();
    }

    /// <summary>
    /// Активирует объявление владельцем.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{listingId}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Activate(string listingId, CancellationToken ct)
    {
        var request = new CatalogService.Grpc.GetRentalListingRequest
        {
            ListingId = listingId
        };

        await _client.ActivateRentalListingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return NoContent();
    }

    /// <summary>
    /// Бронирует слоты доступности владельцем.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="request">Список дат и опциональный идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Результат бронирования.</returns>
    [HttpPost("{listingId}/reserve")]
    [ProducesResponseType(typeof(TryReserveSlotsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TryReserveSlotsResponse>> TryReserveSlots(string listingId, [FromBody] ReservationSlotsRequest request, CancellationToken ct)
    {
        var grpcResponse = await _client.TryReserveSlotsAsync(request.ToGrpc(listingId), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Отменяет бронирование слотов владельцем.
    /// </summary>
    /// <param name="listingId">Идентификатор объявления.</param>
    /// <param name="request">Список дат и опциональный идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{listingId}/cancel-reservation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelReservation(string listingId, [FromBody] ReservationSlotsRequest request, CancellationToken ct)
    {
        await _client.CancelReservationAsync(request.ToGrpc(listingId), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return NoContent();
    }
}