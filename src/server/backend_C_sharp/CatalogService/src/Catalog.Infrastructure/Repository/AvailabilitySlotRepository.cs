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
    
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

    public async Task CreateAsync(Guid listingId, int price, DateOnly date, CancellationToken ct = default)
    {
        await _context.AvailabilitySlots.AddAsync(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            Version = 1,
            IsAvailable = true,
            Price = price
        }, ct);
    }

    public async Task PrepareInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates)
    {
        var busyDaysSet = new HashSet<DateOnly>(busyDates);
        var slots = new List<AvailabilitySlot>(60);

        var startDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        
        for (var i = 0; i < 60; i++)
        {
            var date = startDate.AddDays(i);
            var isBusy = busyDaysSet.Contains(date);
            slots.Add(new AvailabilitySlot
            {
                ListingId = listingId,
                Date = date,
                Version = 1,
                Price = defaultPrice,
                IsAvailable = !isBusy,
                ReservedAt = isBusy ? _timeProvider.GetUtcNow().UtcDateTime : null
            });
        }

        await _context.AvailabilitySlots.AddRangeAsync(slots);
    }

    public async Task<List<AvailabilitySlotDto>> GetTwoMonthSlotsAsync(Guid listingId, CancellationToken ct = default)
    {
        var horizon = Today.AddDays(60);

        return await _context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => 
                s.ListingId == listingId
                && s.Date >= Today 
                && s.Date <= horizon)
            .OrderBy(s => s.Date)
            .Select(s => new AvailabilitySlotDto
            {
                Date = s.Date,
                Version = s.Version,
                Price = s.Price,
                IsAvailable =  s.IsAvailable,
                IsReversible = s.BookingId == null,
                ReservedAt = s.ReservedAt,
                BookingId = s.BookingId
            })
            .ToListAsync(ct);
    }

    public async Task<List<AvailabilitySlot>> GetSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, CancellationToken ct = default)
    {
        var datesList = dates
            .Distinct()
            .ToList();
        
        return await _context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.ListingId == listingId && datesList.Contains(s.Date))
            .ToListAsync(ct);
    }

    public async Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid? bookingId = null, CancellationToken ct = default)
    {
        var datesList = dates
            .Distinct()
            .ToList();

        if (datesList.Count == 0)
        {
            return false;
        }
        
        var listing = await _context.RentalListings
            .FirstOrDefaultAsync(x => x.Id == listingId, ct);

        if (listing == null || !listing.IsActive)
        {
            return false;
        }

        var slots = await _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId && datesList.Contains(s.Date))
            .ToListAsync(ct);

        if (slots.Count != datesList.Count || slots.Any(s => !s.IsAvailable))
        {
            return false;
        }

        var reserveAt = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var slot in slots)
        {
            slot.IsAvailable = false;
            slot.BookingId = bookingId;
            slot.ReservedAt = reserveAt;
            slot.Version++;
        }

        return true;
    }

    public async Task CancelReservationAsync(Guid listingId, IEnumerable<DateOnly> dates,
        Guid? bookingId = null, CancellationToken ct = default)
    {
        var datesList = dates as IReadOnlyCollection<DateOnly> ?? dates.ToList();
        if (datesList.Count == 0)
        {
            return;
        }

        var slots = await _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId)
            .Where(s => datesList.Contains(s.Date))
            .Where(s => !s.IsAvailable)
            .Where(s => s.BookingId == bookingId)
            .ToListAsync(ct);

        if (slots.Count == 0)
        {
            return;
        }

        foreach (var slot in slots)
        {
            slot.IsAvailable = true;
            slot.ReservedAt = null;
            slot.BookingId = null;
            slot.Version++;
        }
    }

    public async Task<bool> UpdateSlotsPriceAsync(Guid listingId, IEnumerable<AvailabilitySlotDto> slots,
        int newPrice, CancellationToken ct = default)
    {
        var slotsList = slots.ToList();
        if (slotsList.Count == 0)
        {
            return false;
        }

        var expectedByDate = slotsList.ToDictionary(s => s.Date, s => s.Version);
        var dates = expectedByDate.Keys.ToList();

        var dbSlots = await _context.AvailabilitySlots
            .Where(s => s.ListingId == listingId)
            .Where(s => dates.Contains(s.Date))
            .ToListAsync(ct);

        if (dbSlots.Count != dates.Count)
        {
            return false;
        }

        foreach (var slot in dbSlots)
        {
            var expectedVersion = expectedByDate[slot.Date];
            if (slot.Version != expectedVersion)
            {
                throw new DbUpdateConcurrencyException($"Availability slot version conflict for listing '{listingId}' and date '{slot.Date}'.");
            }

            if (!slot.IsAvailable)
            {
                return false;
            }

            slot.Price = newPrice;
            slot.Version++;
        }

        return true;
    }

    public async Task<int> RemoveAsync(Guid listingId, DateOnly date, CancellationToken ct = default)
    {
        var slot = await _context.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.ListingId == listingId && s.Date == date, ct);

        if (slot == null)
        {
            return 0;
        }

        _context.AvailabilitySlots.Remove(slot);

        return 1;
    }
}