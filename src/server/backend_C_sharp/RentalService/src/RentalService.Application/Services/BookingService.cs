using Core.SAGA.Contracts.Events;
using MassTransit;
using RentalService.Application.DTO;
using RentalService.Application.Mapper;
using RentalService.Application.Services.Abstractions;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;

namespace RentalService.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingService(IBookingRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> CreateAsync(CreateBookingDto dto)
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
        
        await _publishEndpoint.Publish(@event);
        
        return bookingId;
    }

    public async Task<BookingDto?> GetAsync(Guid id)
    {
        var booking = await _repository.GetAsync(id);

        return booking?.ToDto();
    }

    public async Task<List<BookingDto>> GetAllByTenantAsync(Guid tenantId)
    {
        var bookings = await _repository.GetAllByTenantAsync(tenantId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<List<BookingDto>> GetAllByOwnerAsync(Guid ownerId)
    {
        var bookings = await _repository.GetAllByOwnerAsync(ownerId);
        
        return bookings.Select(x => x.ToDto()).ToList();
    }
}