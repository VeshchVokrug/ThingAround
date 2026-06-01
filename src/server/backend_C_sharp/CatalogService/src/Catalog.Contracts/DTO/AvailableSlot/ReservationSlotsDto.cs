namespace Catalog.Contracts.DTO.AvailableSlot;

public record ReservationSlotsDto
{
    public required Guid ListingId { get; init; }
    public required IEnumerable<DateOnly> Dates { get; init; }
    public Guid? BookingId { get; init; }
};