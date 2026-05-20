using RentalService.Application.DTO;

namespace RentalService.Application.Services.Abstractions;

public interface IBookingService
{
    Task<CreatingBookingResponse> CreateAsync(CreateBookingDto dto, CancellationToken ct = default);
    Task<BookingDto?> GetAsync(Guid id);
    Task<List<BookingDto>> GetAllByTenantAsync(Guid tenantId);
    Task<List<BookingDto>> GetAllByOwnerAsync(Guid ownerId);
    Task<ApprovalBookingResponse> ApproveBookingAsync(Guid bookingId, Guid ownerId, CancellationToken ct = default);
    Task<ApprovalBookingResponse> RejectBookingAsync(Guid bookingId, Guid ownerId, string reason, CancellationToken ct = default);
    Task<ApprovalBookingResponse> CancelBookingAsync(Guid bookingId, Guid tenantId, string reason, CancellationToken ct = default);
}