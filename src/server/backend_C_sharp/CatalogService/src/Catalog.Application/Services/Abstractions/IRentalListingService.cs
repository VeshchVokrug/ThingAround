using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;

namespace Application.Services.Abstractions;

public interface IRentalListingService
{
    Task<Guid> CreateListingAsync(CreateRentalListingDto dto, CancellationToken ct = default);
    Task RemoveListingAsync(Guid listingId, CancellationToken ct = default);
    Task DeactivateAsync(Guid listingId, CancellationToken ct = default);
    Task SystemDeactivateAsync(Guid listingId, CancellationToken ct = default);
    Task UpdateListingAsync(UpdateRentalListingDto dto, CancellationToken ct = default);
    Task<bool> TryReserveSlotsAsync(ReservationSlotsDto dto, CancellationToken ct = default);
    Task CancelReservationAsync(ReservationSlotsDto slots, CancellationToken ct = default);
}