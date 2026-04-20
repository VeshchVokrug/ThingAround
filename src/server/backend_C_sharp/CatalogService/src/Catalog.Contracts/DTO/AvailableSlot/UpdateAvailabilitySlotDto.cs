namespace Catalog.Contracts.DTO.AvailableSlot;

public record UpdateAvailabilitySlotDto
{
    public Guid ListingId { get; init; }
    public DateOnly Date { get; init; }
    public int NewPrice { get; init; }
};