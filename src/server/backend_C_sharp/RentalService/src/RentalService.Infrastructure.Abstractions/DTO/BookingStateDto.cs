using RentalService.Domain.Entity;

namespace RentalService.Infrastructure.Abstractions.DTO;

public record BookingStateDto(
    Guid BookingId,
    Guid ListingId,
    Guid TenantId,
    Guid OwnerId,
    BookingStatus Status);