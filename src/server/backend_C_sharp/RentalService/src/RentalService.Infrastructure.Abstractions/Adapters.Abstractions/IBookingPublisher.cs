using Core.SAGA.Contracts.Events;

namespace RentalService.Infrastructure.Abstractions.Adapters.Abstractions;

public interface IBookingPublisher
{
    Task PublishRequestedAsync(RentalBookingRequestedEvent @event, CancellationToken ct);
    Task PublishApprovedAsync(RentalBookingApprovedEvent @event, CancellationToken ct);
    Task PublishCancelledAsync(RentalBookingCancelledEvent @event, CancellationToken ct);
    Task PublishRejectedAsync(RentalBookingRejectedEvent @event, CancellationToken ct);
}