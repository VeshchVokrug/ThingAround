namespace RentalService.Application.DTO;

public record CreateBookingDto(
    Guid ListingId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal ExpectedPrice);