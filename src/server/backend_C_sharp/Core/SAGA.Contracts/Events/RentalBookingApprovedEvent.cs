namespace Core.SAGA.Contracts.Events;

public record RentalBookingApprovedEvent(
    Guid BookingId,
    Guid OwnerId);