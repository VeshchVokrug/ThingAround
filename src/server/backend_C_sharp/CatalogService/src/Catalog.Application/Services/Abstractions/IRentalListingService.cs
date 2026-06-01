using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;

namespace Application.Services.Abstractions;

public interface IRentalListingService
{
    Task IsOwnerAsync(Guid listingId, Guid userId);
    Task<RentalListingDto> GetAsync(Guid listingId, CancellationToken ct = default);
    Task<PagedResponse<RentalListingCard>> GetAllAsync(RentalFilterRequest request, CancellationToken ct = default);
    Task<List<RentalListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default);
    Task<Guid> CreateListingAsync(CreateRentalListingDto dto, CancellationToken ct = default);
    Task RemoveListingAsync(Guid listingId, CancellationToken ct = default);
    Task DeactivateAsync(Guid listingId, CancellationToken ct = default);
    Task ActivateAsync(Guid listingId, CancellationToken ct = default);
    Task SystemDeactivateAsync(Guid listingId, CancellationToken ct = default);
    Task RemoveImagesAsync(Guid listingId, IEnumerable<string> imagesUrls, CancellationToken ct = default);
    Task UpdateListingAsync(RentalListingDto dto, CancellationToken ct = default);
    Task<bool> TryReserveSlotsAsync(ReservationSlotsDto dto, CancellationToken ct = default);
    Task CancelReservationAsync(ReservationSlotsDto slots, CancellationToken ct = default);
}