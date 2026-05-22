using Application.Services.Abstractions;
using Catalog.Contracts.DTO.AvailableSlot;
using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Consumers;

public class ReleaseSlotConsumer : IConsumer<CatalogReleaseSlots>
{
    private readonly ILogger<ReleaseSlotConsumer> _logger;
    private readonly IRentalListingService _rentalListingService;
    
    public ReleaseSlotConsumer(ILogger<ReleaseSlotConsumer> logger, IRentalListingService rentalListingService)
    {
        _logger = logger;
        _rentalListingService = rentalListingService;
    }

    public async Task Consume(ConsumeContext<CatalogReleaseSlots> context)
    {
        var command = context.Message;
        _logger.LogInformation("Processing released for booking: {BookingId}", command.BookingId); 
        
        try
        {
            await _rentalListingService.CancelReservationAsync(new ReservationSlotsDto
            {
                BookingId = command.BookingId,
                ListingId = command.ListingId,
                Dates = command.Dates,
            }, context.CancellationToken);

            await context.Publish(new CatalogSlotsReleasedEvent(command.BookingId), context.CancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while releasing slots for booking: {BookingId}", command.BookingId);
            throw;
        }
    }
}