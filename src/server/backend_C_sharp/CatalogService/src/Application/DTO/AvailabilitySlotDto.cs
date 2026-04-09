namespace Application.DTO;

public class AvailabilitySlotDto
{
    public DateOnly Date { get; set; }
    public int Price { get; set; }
    public DateTime? ReservedAt { get; set; }
}