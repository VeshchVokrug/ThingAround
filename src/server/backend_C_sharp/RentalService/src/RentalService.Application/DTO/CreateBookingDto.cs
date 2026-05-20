namespace RentalService.Application.DTO;

public record CreateBookingDto(
    Guid ListingId,
    Guid TenantId,
    Guid OwnerId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal ExpectedPrice);