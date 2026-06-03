using Catalog.Contracts.DTO.Listing.Coownership;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class CoownershipListingRepository : ICoownershipListingRepository
{
    private readonly CatalogDbContext _context;

    public CoownershipListingRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<CoownershipListingDto?> GetAsync(Guid listingId, CancellationToken ct = default)
    {
        return await _context.CoownershipListings
            .AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new CoownershipListingDto
            {
                Id = l.Id,
                Version = l.Version,
                TitleSlug = l.TitleSlug,
                CategorySlug = l.CategorySlug,
                Title = l.Title,
                Description = l.Description,
                ImagesUrls = l.ImagesUrls,
                City = l.City,
                SharePrice = l.SharePrice,
                TotalShares = l.TotalShares,
                AvailableShares = l.AvailableShares,
                CatalogListingId = l.CatalogListingId,
                FundingDeadline = l.FundingDeadline,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                IsActive = l.IsActive,
                OwnerId = l.OwnerId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<CoownershipListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _context.CoownershipListings
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CoownershipListingCard(
                x.Id,
                x.Title,
                x.TitleSlug,
                x.ImagesUrls != null ? x.ImagesUrls.FirstOrDefault() : null,
                x.SharePrice,
                x.TotalShares,
                x.AvailableShares,
                x.IsActive
            ))
            .ToListAsync(ct);
    }

    public async Task<bool> IsOwnerAsync(Guid listingId, Guid userId, CancellationToken ct = default)
    {
        return await _context.CoownershipListings
            .AnyAsync(l => l.Id == listingId && l.OwnerId == userId, ct);
    }

    public async Task<Guid> CreateAsync(CoownershipListing listing, CancellationToken ct = default)
    {
        if (listing.Id == Guid.Empty)
        {
            listing.Id = Guid.NewGuid();
        }

        if (listing.AvailableShares == 0)
        {
            listing.AvailableShares = listing.TotalShares;
        }

        await _context.CoownershipListings.AddAsync(listing, ct);

        return listing.Id;
    }

    public async Task<bool> GetActivityStatusAsync(Guid listingId, CancellationToken ct = default)
    {
        var isActive = await _context.CoownershipListings
            .Where(rl => rl.Id == listingId)
            .Select(rl => rl.IsActive)
            .FirstOrDefaultAsync(ct);

        return isActive is true;
    }

    public async Task<bool> UpdateAsync(CoownershipListingDto dto, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.CoownershipListings.AsQueryable();

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
        listing.SharePrice = dto.SharePrice;
        listing.TotalShares = dto.TotalShares;
        listing.AvailableShares = dto.AvailableShares;
        listing.CatalogListingId = dto.CatalogListingId;
        listing.FundingDeadline = dto.FundingDeadline;
        listing.IsActive = dto.IsActive;
        listing.Version++;
        listing.UpdatedAt = DateTime.UtcNow;

        return true;
    }

    public async Task<bool> RemoveAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.CoownershipListings.AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        var listing = await query.FirstOrDefaultAsync(x => x.Id == listingId, ct);

        if (listing == null)
        {
            return false;
        }

        _context.CoownershipListings.Remove(listing);

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.CoownershipListings.AsQueryable();

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

    public async Task<bool> ActivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var query = _context.CoownershipListings.AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        var listing = await query.FirstOrDefaultAsync(x => x.Id == listingId, ct);

        if (listing == null)
        {
            return false;
        }

        listing.IsActive = true;
        listing.Version++;
        listing.UpdatedAt = DateTime.UtcNow;

        return true;
    }
}