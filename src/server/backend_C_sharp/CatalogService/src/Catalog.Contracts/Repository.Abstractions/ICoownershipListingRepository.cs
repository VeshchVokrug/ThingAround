using Catalog.Contracts.DTO.Listing.Coownership;
using Domain.Entity;

namespace Catalog.Contracts.Repository.Abstractions;

public interface ICoownershipListingRepository
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<CoownershipListingDto?> GetAsync(Guid listingId, CancellationToken ct = default);
    Task<List<CoownershipListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default);
    Task<bool> IsOwnerAsync(Guid listingId, Guid userId, CancellationToken ct = default);
    Task<Guid> CreateAsync(CoownershipListing listing, CancellationToken ct = default);
    Task<bool> GetActivityStatusAsync(Guid listingId, CancellationToken ct = default);
    Task<bool> UpdateAsync(CoownershipListingDto dto, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> ActivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
}