using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;

namespace RentalService.Infrastructure.Abstractions.Repository.Abstractions;

public interface IBookingRepository
{
    Task<Booking> GetAsync(Guid id);
    Task<IEnumerable<Booking>> GetAllByOwnerAsync(Guid ownerId);
    Task<IEnumerable<Booking>> GetAllByTenantAsync(Guid userId);
    Task<Guid> AddAsync(Booking booking);
    Task<bool> UpdateAsync(UpdateBookingDto booking);
}