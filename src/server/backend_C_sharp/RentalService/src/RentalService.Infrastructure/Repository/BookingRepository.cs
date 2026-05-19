using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;

namespace RentalService.Infrastructure.Repository;

public class BookingRepository : IBookingRepository
{
    public Task<Booking> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Booking>> GetAllByOwnerAsync(Guid ownerId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Booking>> GetAllByTenantAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> AddAsync(Booking booking)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(UpdateBookingDto booking)
    {
        throw new NotImplementedException();
    }
}