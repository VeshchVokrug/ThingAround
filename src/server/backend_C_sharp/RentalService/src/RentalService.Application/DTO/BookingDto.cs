namespace RentalService.Application.DTO;

public record BookingDto
{
    public Guid Id { get; init; } = Guid.Empty;
    public Guid ListingId { get; init; }
    public Guid TenantId { get; init; }
    public Guid OwnerId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public required string Status { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = default;
    public DateTimeOffset UpdatedAt { get; init; } = default;
    public DateTimeOffset? ExpiresAt { get; init; } = null;
    public uint Version { get; init; } = 0;
    public string? CancellationReason { get; init; }
}