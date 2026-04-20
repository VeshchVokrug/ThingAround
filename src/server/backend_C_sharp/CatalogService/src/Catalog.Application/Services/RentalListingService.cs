using Application.Exceptions;
using Application.Services.Abstractions;
using Catalog.Contracts.Auth;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class RentalListingService : IRentalListingService
{
    private readonly IRentalListingRepository _rentalListingRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ILogger<RentalListingService> _logger;
    private readonly IUserContext _userContext;

    public RentalListingService(IRentalListingRepository rentalListingRepository, ILogger<RentalListingService> logger, IUserContext userContext, IAvailabilitySlotRepository availabilitySlotRepository)
    {
        _rentalListingRepository = rentalListingRepository;
        _logger = logger;
        _userContext = userContext;
        _availabilitySlotRepository = availabilitySlotRepository;
    }

    public async Task<Guid> CreateListingAsync(CreateRentalListingDto dto, CancellationToken ct = default)
    {
        dto.OwnerId = _userContext.UserId;
        return await _rentalListingRepository.CreateAsync(dto, ct);
    }

    public async Task RemoveListingAsync(Guid listingId, CancellationToken ct = default)
    {
        Guid? ownerId = _userContext.IsAdmin 
            ? null 
            : _userContext.UserId;

        var success = await _rentalListingRepository.RemoveAsync(listingId, ownerId, ct);

        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление", listingId);
        }
    }

    public async Task DeactivateAsync(Guid listingId, CancellationToken ct = default)
    {
        Guid? ownerId = _userContext.IsAdmin ? null : _userContext.UserId;

        await ExecuteDeactivationAsync(listingId, ownerId, ct);
    }

    public async Task SystemDeactivateAsync(Guid listingId, CancellationToken ct = default)
    {
        await ExecuteDeactivationAsync(listingId, null, ct);
    }

    public async Task UpdateListingAsync(UpdateRentalListingDto dto, CancellationToken ct = default)
    {
        Guid? ownerId = _userContext.IsAdmin ? null : _userContext.UserId;
        
        var success = await _rentalListingRepository.UpdateAsync(dto, ownerId, ct);
        
        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление", dto.Id);
        }
    }

    public async Task<bool> TryReserveSlotsAsync(ReservationSlotsDto dto, CancellationToken ct = default)
    {
        if (dto.BookingId == null)
        {
            var currentUserId = _userContext.UserId;
            var isOwner = await _rentalListingRepository.IsOwnerAsync(dto.ListingId, currentUserId, ct);
        
            if (!isOwner)
            {
                throw new ForbiddenOrNotFoundException("Объявление", dto.ListingId);
            }
        }
        
        var success = await _availabilitySlotRepository.TryReserveSlotsAsync(dto.ListingId, dto.Dates, dto.BookingId, ct );

        return success 
            ? true
            : throw new AvailabilityConflictException(dto.ListingId, dto.Dates);
    }

    public async Task CancelReservationAsync(ReservationSlotsDto slots, CancellationToken ct = default)
    {
        if (slots.BookingId == null)
        {
            var currentUserId = _userContext.UserId;
            var isOwner = await _rentalListingRepository.IsOwnerAsync(slots.ListingId, currentUserId, ct);
            
            if (!isOwner)
            {
                throw new ForbiddenOrNotFoundException("Объявление", slots.ListingId);
            }
        }
        
        await _availabilitySlotRepository.CancelReservationAsync(slots.ListingId, slots.Dates, slots.BookingId, ct);
    }
    
    private async Task ExecuteDeactivationAsync(Guid listingId, Guid? ownerId, CancellationToken ct)
    {
        var success = await _rentalListingRepository.DeactivateAsync(listingId, ownerId, ct);
        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление", listingId);
        }
    }
}