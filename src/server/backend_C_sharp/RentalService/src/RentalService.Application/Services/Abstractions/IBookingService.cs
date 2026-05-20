using RentalService.Application.DTO;

namespace RentalService.Application.Services.Abstractions;

public interface IBookingService
{
    Task<Guid> CreateAsync(CreateBookingDto dto);
    Task<BookingDto?> GetAsync(Guid id);
    Task<List<BookingDto>> GetAllByTenantAsync(Guid tenantId);
    Task<List<BookingDto>> GetAllByOwnerAsync(Guid ownerId);
}