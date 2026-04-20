using Application.DTO;
using Application.DTO.Listing;
using Application.DTO.Listing.Rental;

namespace Infrastructure.Repository.Abstractions;

public interface IRentalListingRepository
{
    Task<RentalListingDto?> GetAsync(Guid listingId, CancellationToken ct = default);
    Task<IEnumerable<RentalListingCard>> GetAllByUser(Guid ownerId, CancellationToken ct = default);
    Task<PagedResponse<RentalListingCard>> GetFilteredCatalogAsync(RentalFilterRequest request, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateRentalListingDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(UpdateRentalListingDto dto, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
    Task<bool> DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
}