namespace RentalService.Application.DTO;

public record CreatingBookingResponse(
    Guid? BookingId,
    string? CancellationReason = null);