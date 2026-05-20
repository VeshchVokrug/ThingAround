using System.Security.Authentication;
using Application.Exceptions;
using Application.Services.Abstractions;
using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Core.Auth;
using Domain.Entity;
using Microsoft.Extensions.Logging;
using Slugify;

namespace Application.Services;

public class RentalListingService : IRentalListingService
{
    private readonly TimeProvider _timeProvider;
    private readonly IRentalListingRepository _rentalListingRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ILogger<RentalListingService> _logger;
    private readonly IUserContext _userContext;
    private readonly ISlugHelper _slugHelper;
    private readonly Random _random;

    public RentalListingService(IRentalListingRepository rentalListingRepository, ILogger<RentalListingService> logger, IUserContext userContext, IAvailabilitySlotRepository availabilitySlotRepository, TimeProvider timeProvider, ISlugHelper slugHelper)
    {
        _rentalListingRepository = rentalListingRepository;
        _logger = logger;
        _userContext = userContext;
        _availabilitySlotRepository = availabilitySlotRepository;
        _timeProvider = timeProvider;
        _slugHelper = slugHelper;
        _random = new Random();
    }

    public async Task<RentalListingDto> GetAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await _rentalListingRepository.GetAsync(listingId, ct);
        
        if (listing == null)
        {
            throw new ForbiddenOrNotFoundException("Объявление", listingId);
        }
        
        return listing;
    }

    public async Task<PagedResponse<RentalListingCard>> GetAllAsync(RentalFilterRequest request, CancellationToken ct = default)
    {
        if (request.StartDate.HasValue && request.StartDate < DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime)
            || request.EndDate.HasValue && request.EndDate < DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new ArgumentException("Неккоректные даты.");
        }
        
        var page = await _rentalListingRepository.GetFilteredCatalogAsync(request, ct);
        
        return page;
    }

    public async Task<List<RentalListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default)
    {
        var cards = await _rentalListingRepository.GetAllByUserAsync(ownerId, ct);

        return cards;
    }

    public async Task<Guid> CreateListingAsync(CreateRentalListingDto dto, CancellationToken ct = default)
    {
        if (_userContext.UserId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }
        
        var listing = new RentalListing
        {
            TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}",
            Title = dto.Title,
            Description = dto.Description,
            OwnerId = _userContext.UserId,
            CategorySlug = dto.CategorySlug,
            City = dto.City,
            ImagesUrls = dto.ImagesUrls,
            OwnerRating = dto.ManagerRating,
            DefaultPrice = dto.DefaultPrice,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Contact = new ContactInfo
            {
                ManagerId = dto.ManagerId,
                PersonName = dto.ManagerName,
                PersonPhone = dto.ManagerPhone,
                SocialsUrls = dto.ManagerSocialsUrls
            },
        };

        var listingId = await _rentalListingRepository.CreateAsync(listing, dto.BusyDates, ct);
        await _rentalListingRepository.SaveChangesAsync(ct);

        return listingId;
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

        await _rentalListingRepository.SaveChangesAsync(ct);
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

    public async Task UpdateListingAsync(RentalListingDto dto, CancellationToken ct = default)
    {
        Guid? ownerId = _userContext.IsAdmin ? null : _userContext.UserId;

        var hasFullSnapshot = dto.AvailabilitySlots?.Count > 0;
        dto.TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}";
        if (!hasFullSnapshot)
        {
            var updated = await _rentalListingRepository.UpdateAsync(dto, ownerId, ct);
            if (!updated)
            {
                throw new ForbiddenOrNotFoundException("Объявление", dto.Id);
            }

            await _rentalListingRepository.SaveChangesAsync(ct);
            return;
        }

        var currentListing = await _rentalListingRepository.GetAsync(dto.Id, ct);
        if (currentListing == null)
        {
            throw new ForbiddenOrNotFoundException("Объявление", dto.Id);
        }

        if (currentListing.Version != dto.Version)
        {
            throw new OptimisticConcurrencyException($"Конфликт версии объявления '{dto.Id}'.");
        }

        ValidateSnapshotAndCollectSlotChanges(dto.Id, currentListing.AvailabilitySlots!, dto.AvailabilitySlots!,
            out var reserveDates, out var cancelDates);

        if (currentListing.DefaultPrice != dto.DefaultPrice)
        {
            var priceUpdated = await _availabilitySlotRepository.UpdateSlotsPriceAsync(
                dto.Id,
                currentListing.AvailabilitySlots!,
                dto.DefaultPrice,
                ct);

            if (!priceUpdated)
            {
                throw new AvailabilityConflictException(dto.Id, currentListing.AvailabilitySlots!.Select(s => s.Date));
            }
        }

        if (reserveDates.Count > 0)
        {
            var reserved = await _availabilitySlotRepository.TryReserveSlotsAsync(dto.Id, reserveDates, null, ct);
            if (!reserved)
            {
                throw new AvailabilityConflictException(dto.Id, reserveDates);
            }
        }

        if (cancelDates.Count > 0)
        {
            await _availabilitySlotRepository.CancelReservationAsync(dto.Id, cancelDates, null, ct);
        }

        var success = await _rentalListingRepository.UpdateAsync(dto, ownerId, ct);
        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление", dto.Id);
        }

        await _rentalListingRepository.SaveChangesAsync(ct);
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

        if (success)
        {
            await _availabilitySlotRepository.SaveChangesAsync(ct);
        }

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
        await _availabilitySlotRepository.SaveChangesAsync(ct);
    }
    
    private async Task ExecuteDeactivationAsync(Guid listingId, Guid? ownerId, CancellationToken ct)
    {
        var success = await _rentalListingRepository.DeactivateAsync(listingId, ownerId, ct);
        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление", listingId);
        }

        await _rentalListingRepository.SaveChangesAsync(ct);
    }

    private static void ValidateSnapshotAndCollectSlotChanges(
        Guid listingId,
        List<AvailabilitySlotDto> persistedSlots,
        List<AvailabilitySlotDto> requestedSlots,
        out List<DateOnly> reserveDates,
        out List<DateOnly> cancelDates)
    {
        reserveDates = [];
        cancelDates = [];

        var persistedByDate = persistedSlots.ToDictionary(s => s.Date);
        var requestedByDate = requestedSlots.ToDictionary(s => s.Date);

        if (persistedByDate.Count != requestedByDate.Count || persistedByDate.Keys.Except(requestedByDate.Keys).Any())
        {
            throw new AvailabilityConflictException(listingId, requestedByDate.Keys);
        }

        foreach (var (date, requestedSlot) in requestedByDate)
        {
            var persistedSlot = persistedByDate[date];

            if (persistedSlot.Version != requestedSlot.Version)
            {
                throw new OptimisticConcurrencyException($"Конфликт версий слота доступности на '{date}' у объявления '{listingId}'.");
            }

            if (persistedSlot.IsAvailable == requestedSlot.IsAvailable)
            {
                continue;
            }

            var canToggle = persistedSlot.IsReversible && persistedSlot.BookingId == null;
            if (!canToggle)
            {
                throw new AvailabilityConflictException(listingId, [date]);
            }

            if (requestedSlot.IsAvailable)
            {
                cancelDates.Add(date);
            }
            else
            {
                reserveDates.Add(date);
            }
        }
    }
}