namespace RentalService.Domain.Entity;

public class Booking
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public Guid TenantId { get; init; }
    public Guid OwnerId { get; init; }
    public BookingStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public uint Version { get; set; }
    public string? CancellationReason { get; set; }
}