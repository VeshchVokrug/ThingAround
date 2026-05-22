using Catalog.Contracts.DTO.AvailableSlot;

namespace Application.Services.Abstractions;

public interface IAvailabilitySlotService
{
    Task<PriceValidationResult> ValidateExpectedPrice(Guid listingId, decimal expectedPrice, IEnumerable<DateOnly> dates, CancellationToken ct = default);
}