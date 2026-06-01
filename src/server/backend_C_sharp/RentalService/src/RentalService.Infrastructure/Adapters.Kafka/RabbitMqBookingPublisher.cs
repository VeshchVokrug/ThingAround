using Core.SAGA.Contracts.Events;
using MassTransit;
using RentalService.Infrastructure.Abstractions.Adapters.Abstractions;

namespace RentalService.Infrastructure.Adapters.Kafka;

public class RabbitMqBookingPublisher : IBookingPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqBookingPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishRequestedAsync(RentalBookingRequestedEvent @event, CancellationToken ct)
    {
        await _publishEndpoint.Publish(@event, ct);
    }

    public async Task PublishApprovedAsync(RentalBookingApprovedEvent @event, CancellationToken ct)
    {
        await _publishEndpoint.Publish(@event, ct);
    }

    public async Task PublishCancelledAsync(RentalBookingCancelledEvent @event, CancellationToken ct)
    {
        await _publishEndpoint.Publish(@event, ct);
    }

    public async Task PublishRejectedAsync(RentalBookingRejectedEvent @event, CancellationToken ct)
    {
        await _publishEndpoint.Publish(@event, ct);
    }
}