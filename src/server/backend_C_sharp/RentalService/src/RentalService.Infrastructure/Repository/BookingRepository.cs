using Microsoft.EntityFrameworkCore;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;

namespace RentalService.Infrastructure.Repository;

public class BookingRepository : IBookingRepository
{
    private readonly RentalDbContext _context;

    public BookingRepository(RentalDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<Booking?> GetAsync(Guid id)
    { 
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Booking>> GetAllByOwnerAsync(Guid ownerId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.OwnerId == ownerId)
            .OrderByDescending(b => b.CreatedAt)    
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetAllByTenantAsync(Guid tenantId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public async Task<bool> UpdateAsync(UpdateBookingDto dto)
    {
        var booking = await _context.Bookings
            .Where(b => b.Id == dto.Id)
            .Where(b => b.Version == dto.Version)
            .FirstOrDefaultAsync();

        if (booking == null)
        {
            return false;
        }

        booking.Status = dto.Status ?? booking.Status;
        booking.CancellationReason = dto.CancellationReason ?? booking.CancellationReason;
        booking.ExpiresAt = dto.ExpiresAt ?? booking.ExpiresAt;
        booking.UpdatedAt = dto.UpdatedAt;
        booking.Version++;
        
        return true;
    }
}