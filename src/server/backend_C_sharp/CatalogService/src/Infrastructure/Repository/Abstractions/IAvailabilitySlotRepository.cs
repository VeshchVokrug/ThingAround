using Application.DTO;
using Domain.Entity;

namespace Infrastructure.Repository.Abstractions;

public interface IAvailabilitySlotRepository
{
    Task CreateAvailabilitySlotAsync(Guid listingId, int price, DateOnly date, CancellationToken ct);
    Task CreateInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates, CancellationToken ct);
    Task<IEnumerable<AvailabilitySlotDto>> GetAvailabilitySlotsAsync(Guid listingId, CancellationToken ct);
    Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid bookingId, CancellationToken ct);
    Task CancelReservationAsync(Guid listingId, DateOnly date, CancellationToken ct);
    Task<bool> UpdateSlotPriceAsync(Guid listingId, DateOnly date, int newPrice, CancellationToken ct);
    Task<int> RemoveAvailabilitySlotAsync(Guid listingId, DateOnly date, CancellationToken ct);
}   