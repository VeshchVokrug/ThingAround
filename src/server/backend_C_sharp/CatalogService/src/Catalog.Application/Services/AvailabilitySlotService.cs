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

    public async Task<List<AvailabilitySlotDto>> GetAllSlots(Guid listingId, IEnumerable<DateOnly> dates, CancellationToken ct = default)
    {
        var slots = await _repository.GetSlotsAsync(listingId, dates, ct);

        return slots.Select(domain => domain.ToDto()).ToList();
    }
}