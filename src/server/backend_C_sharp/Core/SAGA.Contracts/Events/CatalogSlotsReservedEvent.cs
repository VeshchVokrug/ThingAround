namespace Core.SAGA.Contracts.Events;

public record CatalogSlotsReservedEvent(
    Guid BookingId) : ICatalogEvents;