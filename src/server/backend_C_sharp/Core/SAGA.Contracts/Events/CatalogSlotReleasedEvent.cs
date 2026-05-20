namespace Core.SAGA.Contracts.Events;

public record CatalogSlotReleasedEvent(
    Guid BookingId);