namespace Core.SAGA.Contracts.Commands;

public interface ICatalogCommands
{
    Guid BookingId { get; }
    Guid ListingId { get; }
}