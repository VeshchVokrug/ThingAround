using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;

namespace RentalService.Infrastructure.Abstractions.Repository.Abstractions;

public interface IBookingRepository
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<Booking?> GetAsync(Guid id);
    Task<IEnumerable<Booking>> GetAllByOwnerAsync(Guid ownerId);
    Task<IEnumerable<Booking>> GetAllByTenantAsync(Guid tenantId);
    Task AddAsync(Booking booking);
    Task<bool> UpdateAsync(UpdateBookingDto dto);
}