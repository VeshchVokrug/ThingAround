namespace Domain.Entity;

public class AvailabilitySlot
{
    public Guid ListingId { get; set; }
    public DateOnly Date { get; set; }
    public int Version { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? ReservedAt { get; set; }
    public int Price { get; set; }
    public Guid? BookingId { get; set; }
}