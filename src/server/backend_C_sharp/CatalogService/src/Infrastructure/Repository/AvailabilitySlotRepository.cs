using Application.DTO;
using Domain.Entity;
using Infrastructure.Persistence;
using Infrastructure.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class AvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly CatalogDbContext _context;
    private readonly TimeProvider _timeProvider;

    public AvailabilitySlotRepository(CatalogDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

    public async Task CreateAvailabilitySlotAsync(Guid listingId, int price, DateOnly date, CancellationToken ct)
    {
        await _context.AvailabilitySlots.AddAsync(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            IsAvailable = true,
            Price = price
        }, ct);
    }

    public async Task CreateInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates,
        CancellationToken ct)
    {
        var busyDaysSet = new HashSet<DateOnly>(busyDates);

        var slots = new List<AvailabilitySlot>(60);

        for (var i = 0; i < 60; i++)
        {
            var date = Today.AddDays(i);
            slots.Add(new AvailabilitySlot
            {
                ListingId = listingId,
                Date = date,
                Price = defaultPrice,
                IsAvailable = !busyDaysSet.Contains(date)
            });
        }

        _context.AvailabilitySlots.AddRange(slots);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AvailabilitySlotDto>> GetAvailabilitySlotsAsync(Guid listingId, CancellationToken ct)
    {
        var horizon = Today.AddDays(60);

        return await _context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => 
                s.ListingId == listingId 
                && s.IsAvailable 
                && s.Date >= Today 
                && s.Date <= horizon)
            .OrderBy(s => s.Date)
            .Select(s => new AvailabilitySlotDto
            {
                Date = s.Date,
                Price = s.Price
            })
            .ToListAsync(ct);
    }

    public async Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid bookingId, CancellationToken ct)
    {
        var datesList = dates as IReadOnlyCollection<DateOnly> ?? dates.ToList();
        
        var busiedSlots = await _context.AvailabilitySlots
            .Where(s => 
                s.ListingId == listingId 
                && datesList.Contains(s.Date) 
                && (s.IsAvailable || s.BookingId == bookingId))
            .ExecuteUpdateAsync(sp => sp
                .SetProperty(s => s.IsAvailable, false)
                .SetProperty(s => s.BookingId, bookingId)
                .SetProperty(s => s.ReservedAt, _timeProvider.GetUtcNow().UtcDateTime), ct);
        
        return busiedSlots == datesList.Count;
    }

    public async Task CancelReservationAsync(Guid listingId, DateOnly date, CancellationToken ct)
    {
        await _context.AvailabilitySlots
            .Where(s => 
                s.ListingId == listingId 
                && s.Date == date
                && !s.IsAvailable
                && s.BookingId == null)
            .ExecuteUpdateAsync(sp => sp
                .SetProperty(s => s.IsAvailable, true)
                .SetProperty(s => s.ReservedAt, (DateTime?)null), ct);
    }

    public async Task<bool> UpdateSlotPriceAsync(Guid listingId, DateOnly date, int newPrice, CancellationToken ct)
    {
        var updatedCount = await _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId && s.Date == date)
            .ExecuteUpdateAsync(sp => 
                sp.SetProperty(s => s.Price, newPrice), ct);
        
        return updatedCount > 0;
    }

    public async Task<int> RemoveAvailabilitySlotAsync(Guid listingId, DateOnly date, CancellationToken ct)
    {
        var removedSlots = await _context.AvailabilitySlots
            .Where(s => 
                s.ListingId == listingId 
                && s.Date == date)
            .ExecuteDeleteAsync(ct);
        
        return removedSlots;
    }
}