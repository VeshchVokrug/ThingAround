using Core.SAGA.Contracts.Events;
using MassTransit;
using RentalService.Application.DTO;
using RentalService.Application.Exceptions;
using RentalService.Application.Mapper;
using RentalService.Application.Services.Abstractions;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;

namespace RentalService.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingStatesRepository _bookingStatesRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public BookingService(IBookingRepository bookingRepository, IPublishEndpoint publishEndpoint, IBookingStatesRepository bookingStatesRepository)
    {
        _bookingRepository = bookingRepository;
        _publishEndpoint = publishEndpoint;
        _bookingStatesRepository = bookingStatesRepository;
    }

    public async Task<CreatingBookingResponse> CreateAsync(CreateBookingDto dto, CancellationToken ct)
    {
        var bookingId = Guid.NewGuid();
        
        var @event = new RentalBookingRequestedEvent(
            bookingId,
            dto.ListingId,
            dto.TenantId,
            dto.OwnerId,
            dto.StartDate,
            dto.EndDate,
            dto.ExpectedPrice);
        
        await _publishEndpoint.Publish(@event, ct);
        
        await Task.Delay(150, ct); 
        
        for (var i = 0; i < 15; i++) 
        {
            var statusDto = await _bookingStatesRepository.GetStatusAsync(bookingId);
            if (statusDto is { Status: BookingStatus.PendingApproval }) 
            {
                return new CreatingBookingResponse(bookingId);
            }
            if (statusDto != null && statusDto.Status != BookingStatus.Created)
            {
                return new CreatingBookingResponse(null, statusDto.FailReason);
            }
            await Task.Delay(200, ct);
        }
        
        throw new TimeoutException("Booking not created.");
    }

    public async Task<BookingDto?> GetAsync(Guid id)
    {
        var booking = await _bookingRepository.GetAsync(id);

        return booking?.ToDto();
    }

    public async Task<List<BookingDto>> GetAllByTenantAsync(Guid tenantId)
    {
        var bookings = await _bookingRepository.GetAllByTenantAsync(tenantId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<List<BookingDto>> GetAllByOwnerAsync(Guid ownerId)
    {
        var bookings = await _bookingRepository.GetAllByOwnerAsync(ownerId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<ApprovalBookingResponse> ApproveBookingAsync(Guid bookingId, Guid ownerId, CancellationToken ct)
    {
        await EnsureCanProcessAction(bookingId, ownerId);
        
        await _publishEndpoint.Publish(new RentalBookingApprovedEvent(bookingId, ownerId), ct);
        
        return await WaitForStatusChange(bookingId, BookingStatus.Confirmed, ct);
    }

    public async Task<ApprovalBookingResponse> RejectBookingAsync(Guid bookingId, Guid ownerId, string reason, CancellationToken ct)
    {
        await EnsureCanProcessAction(bookingId, ownerId);
        
        await _publishEndpoint.Publish(new RentalBookingRejectedEvent(bookingId, ownerId, reason), ct);
        
        return await WaitForStatusChange(bookingId, BookingStatus.Rejected, ct);
    }

    public async Task<ApprovalBookingResponse> CancelBookingAsync(Guid bookingId, Guid tenantId, string reason, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetAsync(bookingId);

        if (booking == null || booking.TenantId != tenantId)
        {
            throw new ForbiddenOrNotFoundException("Заявка на бронирование", bookingId);
        }

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Rejected or BookingStatus.Confirmed
            or BookingStatus.Expired)
        {
            return new ApprovalBookingResponse(false, "Заявка уже завершена или отменена ");
        }
        
        await _publishEndpoint.Publish(new RentalBookingCancelledEvent(bookingId, tenantId, reason), ct);
        
        return await WaitForStatusChange(bookingId, BookingStatus.Cancelled, ct);
    }

    private async Task EnsureCanProcessAction(Guid bookingId, Guid ownerId)
    {
        var booking = await _bookingRepository.GetAsync(bookingId);

        if (booking == null || booking.OwnerId != ownerId)
        {
            throw new ForbiddenOrNotFoundException("Заявка на бронирование", bookingId);
        }

        if (booking.Status == BookingStatus.Created)
        {
            throw new InvalidOperationException("Заявка еще формируется. Попробуйте позже.");
        }

        if (booking.Status != BookingStatus.PendingApproval)
        {
            throw new InvalidOperationException("Заявка уже обработана, отменена или просрочена.");
        }
    }
    
    private async Task<ApprovalBookingResponse> WaitForStatusChange(Guid bookingId, BookingStatus targetStatus,
        CancellationToken ct)
    {
        await Task.Delay(150, ct);
    
        for (var i = 0; i < 15; i++) 
        {
            var dto = await _bookingRepository.GetAsync(bookingId);
        
            if (dto == null) 
            {
                await Task.Delay(200, ct);
                continue;
            }

            if (dto.Status == targetStatus)
            {
                return new ApprovalBookingResponse(true);
            }

            if (dto.Status is BookingStatus.Expired or BookingStatus.Cancelled or BookingStatus.Rejected
                or BookingStatus.Confirmed)
            {
                return new ApprovalBookingResponse(false, dto.CancellationReason ?? "Action failed due to status conflict");
            }
            
            await Task.Delay(200, ct);
        }
    
        throw new TimeoutException($"Timed out waiting for booking {bookingId} to reach {targetStatus}");
    }
}