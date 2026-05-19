using RentalService.Domain.Entity;

namespace RentalService.Infrastructure.Abstractions.DTO;

public record UpdateBookingDto
{
    public Guid Id { get; init; }
    public BookingStatus Status { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public uint Version { get; init; }
    public string? CancellationReason { get; init; }
}