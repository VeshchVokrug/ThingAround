using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Infrastructure.Repository;

public class RentalListingRepository : IRentalListingRepository
{
    private readonly CatalogDbContext _context;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    public RentalListingRepository(CatalogDbContext context, IAvailabilitySlotRepository availabilitySlotRepository)
    {
        _context = context;
        _availabilitySlotRepository = availabilitySlotRepository;
    }

    public async Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new RepositoryTransaction(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task<RentalListingDto?> GetAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await _context.RentalListings
            .AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new RentalListingDto
            {
                Id = l.Id,
                Version = l.Version,
                TitleSlug = l.TitleSlug,
                CategorySlug = l.CategorySlug,
                Title = l.Title,
                Description = l.Description,
                ImagesUrls = l.ImagesUrls,
                City = l.City,
                DefaultPrice = l.DefaultPrice,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                OwnerId = l.Contact.ManagerId,
                OwnerRating = l.OwnerRating,
                OwnerName = l.Contact.PersonName,
                OwnerPhone = l.Contact.PersonPhone,
                OwnerSocialsUrls = l.Contact.SocialsUrls
            })
            .FirstOrDefaultAsync(ct);

        if (listing == null)
        {
            return null;
        }
        
        listing.AvailabilitySlots = await _availabilitySlotRepository.GetTwoMonthSlotsAsync(listingId, ct);

        return listing;
    }

    public async Task<List<RentalListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _context.RentalListings
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RentalListingCard(
                x.Id,
                x.Title,
                x.TitleSlug,
                x.ImagesUrls != null ? x.ImagesUrls.FirstOrDefault() : null,
                x.DefaultPrice,
                x.OwnerRating
            ))
            .ToListAsync(ct);
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
        
        var totalCount = await query.CountAsync(ct);
        
        IOrderedQueryable<RentalListing> orderedQuery;
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            orderedQuery = query.OrderByDescending(x => 
                EF.Property<NpgsqlTsVector>(x, "SearchVector")
                    .Rank(EF.Functions.WebSearchToTsQuery("russian", request.SearchTerm)));
        }
        else
        {
            orderedQuery = query.OrderByDescending(x => x.CreatedAt);
        }
        
        var cards = await orderedQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new RentalListingCard(
                x.Id,
                x.Title,
                x.TitleSlug,
                x.ImagesUrls != null ? x.ImagesUrls.FirstOrDefault() : null,
                x.DefaultPrice,
                x.OwnerRating
            ))
            .ToListAsync(ct);

        return new PagedResponse<RentalListingCard>(cards, totalCount, request.PageNumber, request.PageSize, request.City);
    }

    public async Task<bool> IsOwnerAsync(Guid listingId, Guid userId, CancellationToken ct = default)
    {
        return await _context.RentalListings
            .AnyAsync(l => l.Id == listingId && l.OwnerId == userId, ct);
    }

    public async Task<Guid> CreateAsync(RentalListing listing, IEnumerable<DateOnly> busyDates, CancellationToken ct = default)
    {
        if (listing.Id == Guid.Empty)
        {
            listing.Id = Guid.NewGuid();
        }

        var listingId = listing.Id;

        await _context.RentalListings.AddAsync(listing, ct);

        _availabilitySlotRepository.PrepareInitialSlots(listingId, listing.DefaultPrice, busyDates);

        return listingId;
    }

    public async Task<bool> UpdateAsync(UpdateRentalListingDto dto, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.RentalListings
            .Include(x => x.Contact)
            .AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        query = query.Where(x => x.Version == dto.Version);

        var listing = await query.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken: ct);
        if (listing == null)
        {
            return false;
        }

        listing.Title = dto.Title;
        listing.TitleSlug = dto.TitleSlug;
        listing.CategorySlug = dto.CategorySlug;
        listing.Description = dto.Description;
        listing.City = dto.City;
        listing.ImagesUrls = dto.ImagesUrls;
        listing.OwnerRating = dto.OwnerRating;
        listing.Contact = new ContactInfo
        {
            ManagerId = dto.ManagerId,
            PersonName = dto.OwnerName,
            PersonPhone = dto.OwnerPhone,
            SocialsUrls = dto.OwnerSocialsUrls,
        };
        listing.Version++;
        listing.UpdatedAt = DateTime.UtcNow;

        listing.DefaultPrice = dto.DefaultPrice;

        return true;
    }

    public async Task<bool> RemoveAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.RentalListings.AsQueryable();
        
        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }
        
        var listing = await query.FirstOrDefaultAsync(x => x.Id == listingId, ct);

        if (listing == null)
        {
            return false;
        }

        _context.RentalListings.Remove(listing);

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.RentalListings.AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        var listing = await query.FirstOrDefaultAsync(x => x.Id == listingId, ct);

        if (listing == null)
        {
            return false;
        }
        
        listing.IsActive = false;
        listing.Version++;
        listing.UpdatedAt = DateTime.UtcNow;


        return true;
    }
}