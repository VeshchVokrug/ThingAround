using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;

namespace RentalService.Infrastructure.Abstractions.Repository.Abstractions;

public interface IBookingStatesRepository
{
    Task<BookingStatusDto?> GetStatusAsync(Guid bookingId);
}