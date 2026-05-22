using RentalService.Domain.Entity;

namespace RentalService.Infrastructure.Abstractions.DTO;

public record BookingStatusDto(
    BookingStatus Status,
    string? FailReason = null);