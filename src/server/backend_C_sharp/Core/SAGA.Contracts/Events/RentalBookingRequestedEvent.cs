namespace Core.SAGA.Contracts.Events;

public record RentalBookingRequestedEvent(
    Guid BookingId,
    Guid ListingId,
    Guid TenantId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal ExpectedPrice);