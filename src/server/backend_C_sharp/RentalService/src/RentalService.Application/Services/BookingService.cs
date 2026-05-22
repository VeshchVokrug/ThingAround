using System.Security.Authentication;
using Core.Auth;
using Core.SAGA.Contracts.Events;
using MassTransit;
using RentalService.Application.DTO;
using RentalService.Application.Exceptions;
using RentalService.Application.Mapper;
using RentalService.Application.Services.Abstractions;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.Adapters.Abstractions;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;

namespace RentalService.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingStatesRepository _bookingStatesRepository;
    private readonly IBookingPublisher _publishEndpoint;
    private readonly IUserContext _userContext;
    
    public BookingService(IBookingRepository bookingRepository, IBookingStatesRepository bookingStatesRepository, IUserContext userContext, IBookingPublisher publishEndpoint)
    {
        _bookingRepository = bookingRepository;
        _bookingStatesRepository = bookingStatesRepository;
        _userContext = userContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<CreatingBookingResponse> CreateAsync(CreateBookingDto dto, CancellationToken ct)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }       
        
        var bookingId = Guid.NewGuid();
        
        var @event = new RentalBookingRequestedEvent(
            bookingId,
            dto.ListingId,
            _userContext.UserId,
            dto.OwnerId,
            dto.StartDate,
            dto.EndDate,
            dto.ExpectedPrice);
        
        await _publishEndpoint.PublishRequestedAsync(@event, ct);
        await _bookingRepository.SaveChangesAsync(ct);
        
        return new CreatingBookingResponse(bookingId);
    }

    public async Task<BookingDto> GetAsync(Guid id)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var userId = _userContext.UserId;
        
        var booking = await _bookingRepository.GetAsync(id);
        
        if (booking == null || (booking.OwnerId != userId && booking.TenantId != userId))
        {
            throw new ForbiddenOrNotFoundException("Заявка на бронь", id);
        }
        
        return booking.ToDto();
    }

    public async Task<List<BookingDto>> GetAllByTenantAsync()
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var tenantId = _userContext.UserId;
        
        var bookings = await _bookingRepository.GetAllByTenantAsync(tenantId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<List<BookingDto>> GetAllByOwnerAsync()
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var ownerId = _userContext.UserId;        
        
        var bookings = await _bookingRepository.GetAllByOwnerAsync(ownerId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<ApprovalBookingResponse> ApproveBookingAsync(Guid bookingId, CancellationToken ct)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var ownerId = _userContext.UserId;
        
        await EnsureCanProcessAction(bookingId, ownerId);
        
        await _publishEndpoint.PublishApprovedAsync(new RentalBookingApprovedEvent(bookingId, ownerId), ct);
        await _bookingRepository.SaveChangesAsync(ct);
        
        return new ApprovalBookingResponse(true);
    }

    public async Task<ApprovalBookingResponse> RejectBookingAsync(Guid bookingId, string reason, CancellationToken ct)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var ownerId = _userContext.UserId;
        
        await EnsureCanProcessAction(bookingId, ownerId);
        
        await _publishEndpoint.PublishRejectedAsync(new RentalBookingRejectedEvent(bookingId, ownerId, reason), ct);
        await _bookingRepository.SaveChangesAsync(ct);
        
        return new ApprovalBookingResponse(true);
    }

    public async Task<ApprovalBookingResponse> CancelBookingAsync(Guid bookingId, string reason, CancellationToken ct = default)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }   
        
        var tenantId = _userContext.UserId;
        
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
        
        await _publishEndpoint.PublishCancelledAsync(new RentalBookingCancelledEvent(bookingId, tenantId, reason), ct);
        await _bookingRepository.SaveChangesAsync(ct);
        
        return new ApprovalBookingResponse(true);
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
}