namespace Core.SAGA.Contracts.Events;

public record RentalBookingRejectedEvent(
    Guid BookingId,
    Guid OwnerId,
    string Reason) : IRentalEvents;