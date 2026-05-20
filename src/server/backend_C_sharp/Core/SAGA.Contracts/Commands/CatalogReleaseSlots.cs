namespace Core.SAGA.Contracts.Commands;

public record CatalogReleaseSlots(
    Guid BookingId,
    Guid ListingId,
    IEnumerable<DateOnly> Dates);