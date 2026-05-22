using RentalService.Application.DTO;

namespace RentalService.Application.Services.Abstractions;

public interface IBookingService
{
    Task<CreatingBookingResponse> CreateAsync(CreateBookingDto dto, CancellationToken ct = default);
    Task<BookingDto> GetAsync(Guid id);
    Task<List<BookingDto>> GetAllByTenantAsync();
    Task<List<BookingDto>> GetAllByOwnerAsync();
    Task<ApprovalBookingResponse> ApproveBookingAsync(Guid bookingId, CancellationToken ct = default);
    Task<ApprovalBookingResponse> RejectBookingAsync(Guid bookingId, string reason, CancellationToken ct = default);
    Task<ApprovalBookingResponse> CancelBookingAsync(Guid bookingId, string reason, CancellationToken ct = default);
}