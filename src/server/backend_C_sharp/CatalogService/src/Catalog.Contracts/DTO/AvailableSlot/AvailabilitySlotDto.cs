namespace Catalog.Contracts.DTO.AvailableSlot;

public record AvailabilitySlotDto
{
    public DateOnly Date { get; init; }
    public int Version { get; init; }
    public int? Price { get; init; }
    public DateTime? ReservedAt { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsReversible { get; init; }
    public Guid? BookingId { get; init; }
}