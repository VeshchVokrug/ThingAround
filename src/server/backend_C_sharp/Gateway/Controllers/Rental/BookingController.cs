using Gateway.Mappers.IdentityProfile;
using Gateway.Mappers.Rental;
using Gateway.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalService.Grpc;
using RentalClient = RentalService.Grpc.RentalService.RentalServiceClient;
    
namespace Gateway.Controllers.Rental;

[ApiController]
[Route("api/v1/rental/bookings")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly RentalClient _client;

    public BookingController(RentalClient client)
    {
        _client = client;
    }
    
    /// <summary>
    /// Создает новое бронирование.
    /// </summary>
    /// <param name="request">Данные для создания бронирования.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Идентификатор созданного бронирования.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateBookingResponseDto>> Create([FromBody] CreateBookingRequestDto request, CancellationToken ct)
    {
        var grpcResponse = await _client.CreateBookingAsync(request.ToGrpc(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору. Бронирование будет доступно только в том случае, если пользователь является арендодателем или арендатором.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>Информация о бронировании.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(string id, CancellationToken ct)
    {
        var request = new GetBookingByIdRequest { BookingId = id };
        var grpcResponse = await _client.GetBookingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Возвращает список всех бронирований текущего пользователя (как арендатора).
    /// </summary>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpGet("as-tenant")]
    [ProducesResponseType(typeof(BookingListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingListDto>> GetByTenant(CancellationToken ct)
    {
        var grpcResponse = await _client.GetBookingsByTenantAsync(new Empty(),Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Возвращает список бронирований для владельца объявлений (как арендодателя).
    /// </summary>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpGet("as-owner")]
    [ProducesResponseType(typeof(BookingListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingListDto>> GetByOwner(CancellationToken ct)
    {
        var grpcResponse = await _client.GetBookingsByOwnerAsync(new Empty(), Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }
    
    
    /// <summary>
    /// Подтверждает бронирование арендодателем.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalResponseDto>> Approve(string id, CancellationToken ct)
    {
        var request = new GetBookingByIdRequest { BookingId = id };
        var grpcResponse = await _client.ApproveBookingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Отклоняет бронирование арендодателем.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="reason">Причина отклонения.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalResponseDto>> Reject(string id, [FromBody] string reason, CancellationToken ct)
    {
        var request = new ChangeStatusWithReasonRequest { BookingId = id, Reason = reason };
        var grpcResponse = await _client.RejectBookingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }

    /// <summary>
    /// Отменяет бронирование арендатором.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="reason">Причина отмены.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalResponseDto>> Cancel(string id, [FromBody] string reason, CancellationToken ct)
    {
        var request = new ChangeStatusWithReasonRequest { BookingId = id, Reason = reason };
        var grpcResponse = await _client.CancelBookingAsync(request, Request.ToAuthorizationMetadata(), cancellationToken: ct);
        return Ok(grpcResponse.ToDto());
    }
}