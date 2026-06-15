using Catalog.Contracts.DTO.Listing.Coownership;

namespace Application.Services.Abstractions;

public interface ICoownershipListingService
{
    Task IsOwnerAsync(Guid listingId, Guid userId);
    Task<CoownershipListingDto> GetAsync(Guid listingId, CancellationToken ct = default);
    Task<List<CoownershipListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default);
    Task UpsertListingAsync(CoownershipListingDto dto, CancellationToken ct = default);
    Task RemoveListingAsync(Guid listingId, int? version = null, Guid? ownerId = null, CancellationToken ct = default);
    Task DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
    Task ActivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default);
}