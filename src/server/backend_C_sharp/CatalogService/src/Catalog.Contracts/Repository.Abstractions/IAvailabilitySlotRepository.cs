using Catalog.Contracts.DTO.AvailableSlot;
using Domain.Entity;

namespace Catalog.Contracts.Repository.Abstractions;

public interface IAvailabilitySlotRepository
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task CreateAsync(Guid listingId, int price, DateOnly date, CancellationToken ct = default);
    Task PrepareInitialSlotsAsync(Guid listingId, int defaultPrice, IEnumerable<DateOnly> busyDates);
    Task<List<AvailabilitySlotDto>> GetTwoMonthSlotsAsync(Guid listingId, CancellationToken ct = default);
    Task<List<AvailabilitySlot>> GetSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, CancellationToken ct = default);
    Task<bool> TryReserveSlotsAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid? bookingId = null, CancellationToken ct = default);
    Task CancelReservationAsync(Guid listingId, IEnumerable<DateOnly> dates, Guid? bookingId = null, CancellationToken ct = default);
    Task<bool> UpdateSlotsPriceAsync(Guid listingId, IEnumerable<AvailabilitySlotDto> slots, int newPrice, CancellationToken ct = default);
    Task<int> RemoveAsync(Guid listingId, DateOnly date, CancellationToken ct = default);
}