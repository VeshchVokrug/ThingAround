using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using Infrastructure.Persistence;
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

    public async Task CreateAvailabilitySlotAsync(Guid listingId, int price, DateOnly date, CancellationToken ct = default)
    {
        await _context.AvailabilitySlots.AddAsync(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            IsAvailable = true,
            Price = price
        }, ct);
        
        await _context.SaveChangesAsync(ct);
    }

    public async Task CreateInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates,
        CancellationToken ct = default)
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

        await _context.AvailabilitySlots.AddRangeAsync(slots, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailabilitySlotsAsync(Guid listingId, CancellationToken ct = default)
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
            .Select(s => new AvailableSlotDto
            {
                Date = s.Date,
                Price = s.Price,
                IsReversible = s.IsAvailable && s.BookingId == null
            })
            .ToListAsync(ct);
    }

    public async Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid? bookingId = null, CancellationToken ct = default)
    {
        var datesList = dates as IReadOnlyCollection<DateOnly> ?? dates.ToList();
        
        await _context.AvailabilitySlots
            .Where(s => 
                s.ListingId == listingId 
                && datesList.Contains(s.Date) 
                && s.IsAvailable)
            .ExecuteUpdateAsync(sp => sp
                .SetProperty(s => s.IsAvailable, false)
                .SetProperty(s => s.BookingId, bookingId)
                .SetProperty(s => s.ReservedAt, _timeProvider.GetUtcNow().UtcDateTime), ct);
        
        var totalReserved = await _context.AvailabilitySlots
            .CountAsync(s => 
                s.ListingId == listingId 
                && datesList.Contains(s.Date) 
                && s.IsAvailable == false
                && s.BookingId == bookingId, ct);
        
        return totalReserved == datesList.Count;
    }

    public async Task CancelReservationAsync(Guid listingId, IEnumerable<DateOnly> dates,
        Guid? bookingId = null, CancellationToken ct = default)
    {
        var datesList = dates as IReadOnlyCollection<DateOnly> ?? dates.ToList();
        if (datesList.Count == 0)
        {
            return;
        }

        var query = _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId)
            .Where(s => datesList.Contains(s.Date))
            .Where(s => !s.IsAvailable)
            .Where(s => s.BookingId == bookingId);

        await query.ExecuteUpdateAsync(sp => sp
            .SetProperty(s => s.IsAvailable, true)
            .SetProperty(s => s.ReservedAt, (DateTime?)null)
            .SetProperty(s => s.BookingId, (Guid?)null), ct);
    }

    public async Task<bool> UpdateSlotPriceAsync(Guid listingId, DateOnly date, int newPrice, CancellationToken ct = default)
    {
        var updatedCount = await _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId && s.Date == date)
            .ExecuteUpdateAsync(sp => 
                sp.SetProperty(s => s.Price, newPrice), ct);
        
        return updatedCount > 0;
    }

    public async Task<int> RemoveAvailabilitySlotAsync(Guid listingId, DateOnly date, CancellationToken ct = default)
    {
        var removedSlots = await _context.AvailabilitySlots
            .Where(s => 
                s.ListingId == listingId 
                && s.Date == date)
            .ExecuteDeleteAsync(ct);
        
        return removedSlots;
    }
}