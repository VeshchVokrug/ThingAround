using Application.DTO;
using Domain.Entity;

namespace Infrastructure.Repository.Abstractions;

public interface IAvailabilitySlotRepository
{
    Task CreateAvailabilitySlotAsync(Guid listingId, int price, DateOnly date, CancellationToken ct = default);
    Task CreateInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates, CancellationToken ct = default);
    Task<IEnumerable<AvailableSlotDto>> GetAvailabilitySlotsAsync(Guid listingId, CancellationToken ct = default);
    Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid bookingId, CancellationToken ct = default);
    Task CancelReservationAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid? bookingId = null, CancellationToken ct = default);
    Task<bool> UpdateSlotPriceAsync(Guid listingId, DateOnly date, int newPrice, CancellationToken ct = default);
    Task<int> RemoveAvailabilitySlotAsync(Guid listingId, DateOnly date, CancellationToken ct = default);
}