using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class ListingQueryRepository : IListingQueryRepository
{
    private readonly CatalogDbContext _context;

    public ListingQueryRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<List<ListingPrice>> GetAllRentalListingPrices()
    {
        return await _context.RentalListings
            .AsNoTracking()
            .Where(rl => rl.IsActive)
            .Select(rl => new ListingPrice(rl.Id, rl.DefaultPrice))
            .ToListAsync();
    }

    public async Task<DateOnly> GetEarliestSlotDate(Guid listingId)
    {
        return await _context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.ListingId == listingId)
            .OrderBy(s => s.Date)
            .Select(s => s.Date)
            .FirstOrDefaultAsync();
    }

    public async Task<DateOnly> GetLatestSlotDate(Guid listingId)
    {
        return await _context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.ListingId == listingId)
            .OrderByDescending(s => s.Date)
            .Select(s => s.Date)
            .FirstOrDefaultAsync();
    }
}