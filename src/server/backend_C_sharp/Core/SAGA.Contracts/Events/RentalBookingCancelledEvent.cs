namespace Core.SAGA.Contracts.Events;

public record RentalBookingCancelledEvent(
    Guid BookingId,
    Guid TenantId,
    string Reason) : IRentalEvents;