using Application.Mappers;
using Application.Services.Abstractions;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.Repository.Abstractions;

namespace Application.Services;

public class AvailabilitySlotService : IAvailabilitySlotService
{
    private readonly IAvailabilitySlotRepository _repository;

    public AvailabilitySlotService(IAvailabilitySlotRepository repository)
    {
        _repository = repository;
    }

    public async Task<PriceValidationResult> ValidateExpectedPrice(Guid listingId, decimal expectedPrice, IEnumerable<DateOnly> dates, CancellationToken ct = default)
    {
        var slots = await _repository.GetSlotsAsync(listingId, dates, ct);

        var actualPrice = slots.Select(s => s.Price).Sum();

        return actualPrice != expectedPrice 
            ? new PriceValidationResult(false, actualPrice) 
            : new PriceValidationResult(true);
    }
}