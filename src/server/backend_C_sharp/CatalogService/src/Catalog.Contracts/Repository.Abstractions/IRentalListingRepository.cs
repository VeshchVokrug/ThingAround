using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.Listing.Rental;
using Domain.Entity;

namespace Catalog.Contracts.Repository.Abstractions;

public interface IRentalListingRepository
{
    Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<RentalListingDto?> GetAsync(Guid listingId, CancellationToken ct = default);
    Task<List<RentalListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default);
    Task<PagedResponse<RentalListingCard>> GetFilteredCatalogAsync(RentalFilterRequest request, CancellationToken ct = default);
    Task<bool> IsOwnerAsync(Guid listingId, Guid userId, CancellationToken ct = default);
    Task<Guid> CreateAsync(RentalListing listing, IEnumerable<DateOnly> busyDates, CancellationToken ct = default);
    Task<bool> UpdateAsync(UpdateRentalListingDto dto, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
}