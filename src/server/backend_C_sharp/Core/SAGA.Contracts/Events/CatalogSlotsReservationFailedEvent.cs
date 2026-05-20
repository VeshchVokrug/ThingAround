namespace Core.SAGA.Contracts.Events;

public record CatalogSlotsReservationFailedEvent(
    Guid BookingId,
    string Reason);