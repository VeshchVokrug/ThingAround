namespace Application.DTO;

public record AvailableSlotDto
{
    public DateOnly Date { get; init; }
    public int Price { get; init; }
    public DateTime? ReservedAt { get; init; }
    public required bool IsReversible { get; init; }
}