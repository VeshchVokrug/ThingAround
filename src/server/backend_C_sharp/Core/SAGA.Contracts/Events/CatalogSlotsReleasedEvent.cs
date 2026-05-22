namespace Core.SAGA.Contracts.Events;

public record CatalogSlotsReleasedEvent(
    Guid BookingId) : ICatalogEvents;