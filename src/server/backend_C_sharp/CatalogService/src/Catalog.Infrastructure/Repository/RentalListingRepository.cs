using Application.DTO;
using Infrastructure.Persistence;
using Infrastructure.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Infrastructure.Repository;

public class RentalListingRepository : IRentalListingRepository
{
    private readonly CatalogDbContext _context;

    public RentalListingRepository(CatalogDbContext context)
    {
        _context = context;
    }
    
    public async Task<PagedResponse<RentalListingCard>> GetFilteredCatalogAsync(RentalFilterRequest request, CancellationToken ct = default)
    {
        var query = _context.RentalListings.AsNoTracking().AsQueryable();
        
        query = query.Where(x => x.IsActive);
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => 
                EF.Property<NpgsqlTsVector>(x, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery("russian", request.SearchTerm)));
        }
        
        if (!string.IsNullOrEmpty(request.City))
        {
            query = query.Where(x => x.City == request.City);
        }

        if (!string.IsNullOrEmpty(request.CategorySlug))
        {
            query = query.Where(x => x.CategorySlug == request.CategorySlug);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.DefaultPrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.DefaultPrice <= request.MaxPrice.Value);
        }

        if (request.MinRating.HasValue)
        {
            query = query.Where(x => x.OwnerRating >= request.MinRating.Value);
        }

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var daysCount = request.EndDate.Value.DayNumber - request.StartDate.Value.DayNumber + 1;
            
            query = query.Where(x => _context.AvailabilitySlots
                .Count(slot => 
                    slot.ListingId == x.Id
                    && slot.Date >= request.StartDate.Value 
                    && slot.Date <= request.EndDate.Value 
                    && slot.IsAvailable) == daysCount);
        }
        
        var pagedQuery = query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                Listing = x,
                TotalCount = query.Count()
            })
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var result = await pagedQuery.ToListAsync(ct);
        
        var totalCount = result.FirstOrDefault()?.TotalCount ?? 0;
        var cards = result.Select(r => new RentalListingCard(
            ListingId: r.Listing.Id,
            Title: r.Listing.Title,
            ImageUrl: r.Listing.ImagesUrls?.FirstOrDefault(),
            PricePerDay: r.Listing.DefaultPrice,
            OwnerRating: r.Listing.OwnerRating
        )).ToList();

        return new PagedResponse<RentalListingCard>(cards, totalCount, request.PageNumber, request.PageSize, request.City);
    }
}