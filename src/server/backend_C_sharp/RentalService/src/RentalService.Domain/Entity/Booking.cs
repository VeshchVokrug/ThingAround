namespace RentalService.Domain.Entity;

public record Booking
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public Guid TenantId { get; init; }
    public Guid OwnerId { get; init; }
    public BookingStatus Status { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public uint Version { get; init; }
    public string? CancellationReason { get; init; }
}