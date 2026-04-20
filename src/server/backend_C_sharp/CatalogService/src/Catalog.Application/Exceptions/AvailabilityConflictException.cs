namespace Application.Exceptions;

public class AvailabilityConflictException(Guid listingId, IEnumerable<DateOnly> dates)
    : Exception($"Один или несколько слотов для объявления {listingId} недоступны или уже забронированы.")
{
    public Guid ListingId { get; } = listingId;
    public IEnumerable<DateOnly> RequestedDates { get; } = dates;
}