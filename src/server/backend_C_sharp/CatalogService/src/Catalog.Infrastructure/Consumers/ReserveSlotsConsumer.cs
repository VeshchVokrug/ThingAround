using Application.Exceptions;
using Application.Services.Abstractions;
using Catalog.Contracts.DTO.AvailableSlot;
using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Consumers;

public class ReserveSlotsConsumer : IConsumer<CatalogReserveSlots>
{
    private readonly ILogger<ReserveSlotsConsumer> _logger;
    private readonly IAvailabilitySlotService _availabilitySlotService;
    private readonly IRentalListingService _rentalListingService;
    private readonly TimeProvider _timeProvider;
    
    public ReserveSlotsConsumer(ILogger<ReserveSlotsConsumer> logger,
        IAvailabilitySlotService availabilitySlotService, IRentalListingService rentalListingService, TimeProvider timeProvider)
    {
        _logger = logger;
        _availabilitySlotService = availabilitySlotService;
        _rentalListingService = rentalListingService;
        _timeProvider = timeProvider;
    }

    public async Task Consume(ConsumeContext<CatalogReserveSlots> context)
    {
        var command = context.Message;
        _logger.LogInformation("Processing reservation for booking: {BookingId}", command.BookingId);

        try
        {
            await _rentalListingService.IsOwnerAsync(command.ListingId, command.OwnerId);
        }
        catch (ForbiddenOrNotFoundException e)
        {
            await context.Publish(new CatalogSlotsReservationFailedEvent(command.BookingId,
                $"Указанный OwnerId {command.OwnerId} не имеет доступа к объявлению {command.ListingId}."), context.CancellationToken);
            
            return;
        }
        
        var priceValidation = await _availabilitySlotService.ValidateExpectedPrice(command.ListingId, command.ExpectedPrice, command.Dates, context.CancellationToken);
        
        if (!priceValidation.IsMatch)
        {
            _logger.LogError("Price validation failed for {BookingId}", command.BookingId);
            await context.Publish(new CatalogSlotsReservationFailedEvent(command.BookingId,
                $"Текущая цена '{priceValidation.ActualPrice}'руб. отличается от ожидаемой '{command.ExpectedPrice}руб.'"),
                context.CancellationToken);
            
            return;
        }

        try
        {
            await _rentalListingService.TryReserveSlotsAsync(new ReservationSlotsDto
            {
                BookingId = command.BookingId,
                ListingId = command.ListingId,
                Dates = command.Dates
            }, context.CancellationToken);
        }
        catch (AvailabilityConflictException e)
        {
            _logger.LogError("Reservation failed for {BookingId}", command.BookingId);
            await context.Publish(new CatalogSlotsReservationFailedEvent(command.BookingId,
                $"{e.Message} [{_timeProvider.GetUtcNow()}]."), context.CancellationToken);
            
            return;
        }

        _logger.LogInformation("Successfully reserved slots for {BookingId}", command.BookingId);
        await context.Publish(new CatalogSlotsReservedEvent(command.BookingId), context.CancellationToken);
    }
}