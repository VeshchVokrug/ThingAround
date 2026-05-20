namespace Core.SAGA.Contracts.Commands;

public record CatalogReserveSlots(
    Guid BookingId,
    Guid ListingId,
    IEnumerable<DateOnly> Dates) : ICatalogCommands;