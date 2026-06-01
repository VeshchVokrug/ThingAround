namespace Core.SAGA.Contracts.Commands;

public record CatalogReserveSlots(
    Guid BookingId,
    Guid ListingId,
    Guid OwnerId,
    decimal ExpectedPrice,
    IEnumerable<DateOnly> Dates) : ICatalogCommands;