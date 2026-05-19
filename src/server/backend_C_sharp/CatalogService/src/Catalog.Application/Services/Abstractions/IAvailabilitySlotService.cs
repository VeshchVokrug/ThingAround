using Catalog.Contracts.DTO.AvailableSlot;

namespace Application.Services.Abstractions;

public interface IAvailabilitySlotService
{
    Task<List<AvailabilitySlotDto>> GetAllSlots(Guid listingId, IEnumerable<DateOnly> dates, CancellationToken ct = default);
}