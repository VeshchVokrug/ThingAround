using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;

namespace RentalService.Application.SAGA;

public class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BookingStateMachine> _logger;
    private static readonly TimeSpan OwnerApprovalTimeoutWindow = TimeSpan.FromHours(24);

    private State AwaitingCatalogReservation { get; set; } = null!;
    private State AwaitingOwnerApproval { get; set; } = null!;

    private Event<RentalBookingRequestedEvent> OnSagaStarted { get; set; } = null!;
    private Event<CatalogSlotsReservedEvent> OnCatalogSlotsReserved { get; set; } = null!;
    private Event<CatalogSlotsReservationFailedEvent> OnCatalogSlotsReservationFailed { get; set; } = null!;
    private Event<RentalBookingApprovedEvent> OnRentalBookingApproved { get;  set; } = null!;
    private Event<RentalBookingRejectedEvent> OnRentalBookingRejected { get; set; } = null!;
    private Event<RentalBookingCancelledEvent> OnRentalBookingCancelled { get; set; } = null!;
    private Schedule<BookingState, RentalBookingExpiredEvent> OwnerApprovalExpired { get; set; } = null!;
    
    public BookingStateMachine(TimeProvider timeProvider, ILogger<BookingStateMachine> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;

        State(() => AwaitingCatalogReservation);
        State(() => AwaitingOwnerApproval);

        Event(() => OnSagaStarted, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => OnCatalogSlotsReserved, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => OnCatalogSlotsReservationFailed, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => OnRentalBookingApproved, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => OnRentalBookingRejected, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => OnRentalBookingCancelled, x => x.CorrelateById(m => m.Message.BookingId));
        
        Schedule(() => OwnerApprovalExpired, x => x.BookingExpiredTokenId, x =>
        {
            x.Delay = OwnerApprovalTimeoutWindow;
            x.Received = e => e.CorrelateById(m => m.Message.BookingId);
        });

        InstanceState(x => x.CurrentState);

        Initially(
            When(OnSagaStarted)
                .Then(LogSagaState)
                .Then(InitializeSaga)
                .ThenAsync(CreateBookingInDb)
                .ThenAsync(SendCatalogReserveSlots)
                .TransitionTo(AwaitingCatalogReservation)
        );

        During(AwaitingCatalogReservation,
            When(OnCatalogSlotsReserved)
                .Then(LogSagaState)
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.PendingApproval))
                .Schedule(OwnerApprovalExpired, context => new RentalBookingExpiredEvent(context.Saga.CorrelationId))
                .TransitionTo(AwaitingOwnerApproval),

            When(OnCatalogSlotsReservationFailed)
                .Then(LogSagaState)
                .Then(context => context.Saga.FailureReason = "Catalog failed to reserve slots")
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.Rejected, context.Saga.FailureReason))
                .TransitionTo(Final)
        );

        During(AwaitingOwnerApproval,
            When(OnRentalBookingApproved)
                .Then(LogSagaState)
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.Confirmed))
                .Unschedule(OwnerApprovalExpired)
                .TransitionTo(Final),

            When(OnRentalBookingRejected)
                .Then(LogSagaState)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.Rejected, context.Message.Reason))
                .ThenAsync(SendCatalogReleaseSlots)
                .Unschedule(OwnerApprovalExpired)
                .TransitionTo(Final),
            
            When(OwnerApprovalExpired.Received)
                .Then(LogSagaState)
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.Expired))
                .ThenAsync(SendCatalogReleaseSlots)
                .TransitionTo(Final)
        );
        
        During(AwaitingCatalogReservation, AwaitingOwnerApproval,
            When(OnRentalBookingCancelled)
                .Then(LogSagaState)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .ThenAsync(context => UpdateBookingStatus(context, BookingStatus.Cancelled, context.Message.Reason))
                .ThenAsync(SendCatalogReleaseSlots)
                .Unschedule(OwnerApprovalExpired)
                .TransitionTo(Final)
        );
        
        SetCompletedWhenFinalized();
    }

    private void InitializeSaga(BehaviorContext<BookingState, RentalBookingRequestedEvent> context)
    {
        context.Saga.CorrelationId = context.CorrelationId ?? context.Saga.CorrelationId;
        context.Saga.ListingId = context.Message.ListingId;
        context.Saga.TenantId = context.Message.TenantId;
        context.Saga.OwnerId = context.Message.OwnerId;
        context.Saga.StartDate = context.Message.StartDate;
        context.Saga.EndDate = context.Message.EndDate;
        context.Saga.TotalPrice = context.Message.ExpectedPrice;
        context.Saga.Status = BookingStatus.Created;
        context.Saga.BookingVersion = 1;
        context.Saga.CreatedAt = _timeProvider.GetUtcNow();
    }

    private async Task CreateBookingInDb(BehaviorContext<BookingState, RentalBookingRequestedEvent> context)
    {
        var bookingRepository = context.GetPayload<IServiceProvider>().GetRequiredService<IBookingRepository>();
        
        var booking = new Booking
        {
            Id = context.Saga.CorrelationId,
            ListingId = context.Saga.ListingId,
            TenantId = context.Saga.TenantId,
            OwnerId = context.Saga.OwnerId,
            Status = context.Saga.Status,
            StartDate = context.Saga.StartDate,
            EndDate = context.Saga.EndDate,
            TotalPrice = context.Saga.TotalPrice,
            CreatedAt = context.Saga.CreatedAt,
            Version = context.Saga.BookingVersion
        };
        await bookingRepository.AddAsync(booking);
    }

    private Task SendCatalogReserveSlots(BehaviorContext<BookingState, RentalBookingRequestedEvent> context)
    {
        return context.Publish(new CatalogReserveSlots(
            context.Saga.CorrelationId,
            context.Saga.ListingId,
            GenerateDates(context.Saga.StartDate, context.Saga.EndDate)));
    }

    private Task SendCatalogReleaseSlots<T>(BehaviorContext<BookingState, T> context) where T : class
    {
        return context.Publish(new CatalogReleaseSlots(
            context.Saga.CorrelationId,
            context.Saga.ListingId,
            GenerateDates(context.Saga.StartDate, context.Saga.EndDate)));
    }

    private async Task UpdateBookingStatus<T>(BehaviorContext<BookingState, T> context, BookingStatus status, string? cancellationReason = null) where T : class
    {
        context.Saga.Status = status;
        
        var bookingRepository = context.GetPayload<IServiceProvider>().GetRequiredService<IBookingRepository>();
        
        var updated = await bookingRepository.UpdateAsync(new UpdateBookingDto
        {
            Id = context.Saga.CorrelationId,
            Status = status,
            UpdatedAt = _timeProvider.GetUtcNow(),
            ExpiresAt = status == BookingStatus.Expired ? _timeProvider.GetUtcNow() : null,
            Version = context.Saga.BookingVersion,
            CancellationReason = cancellationReason
        });

        if (!updated)
        {
            throw new Exception($"Failed to update booking {context.Saga.CorrelationId} in DB.");
        }
        context.Saga.BookingVersion++;
    }

    private void LogSagaState<TEvent>(BehaviorContext<BookingState, TEvent> context) where TEvent : class
    {
        _logger.LogInformation("{BookingStateName}  correlationId: {SagaCorrelationId}  event: {EventName}", nameof(BookingState), context.Saga.CorrelationId, context.Event.Name);
        context.Saga.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
    }

    private IEnumerable<DateOnly> GenerateDates(DateOnly start, DateOnly end)
    {
        var dates = new List<DateOnly>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            dates.Add(date);
        }
        return dates;
    }
}
