namespace Core.SAGA.Contracts.Events;

public record RentalBookingExpiredEvent(
    Guid BookingId);