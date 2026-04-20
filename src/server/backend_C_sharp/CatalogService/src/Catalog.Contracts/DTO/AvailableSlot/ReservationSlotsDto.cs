namespace Catalog.Contracts.DTO.AvailableSlot;

public record ReservationSlotsDto
{
    public Guid ListingId { get; init; }
    public IEnumerable<DateOnly> Dates { get; init; }
    public Guid? BookingId { get; init; }
};