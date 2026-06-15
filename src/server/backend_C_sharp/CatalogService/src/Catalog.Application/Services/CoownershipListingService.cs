using System.Security.Authentication;
using Application.Exceptions;
using Application.Services.Abstractions;
using Catalog.Contracts.DTO.Listing.Coownership;
using Catalog.Contracts.Repository.Abstractions;
using Core.Auth;
using Domain.Entity;
using Microsoft.Extensions.Logging;
using Slugify;

namespace Application.Services;

public class CoownershipListingService : ICoownershipListingService
{
    private readonly TimeProvider _timeProvider;
    private readonly ICoownershipListingRepository _repository;
    private readonly ILogger<CoownershipListingService> _logger;
    private readonly IUserContext _userContext;
    private readonly ISlugHelper _slugHelper;
    private readonly Random _random;

    public CoownershipListingService(
        ICoownershipListingRepository repository,
        ILogger<CoownershipListingService> logger,
        IUserContext userContext,
        TimeProvider timeProvider,
        ISlugHelper slugHelper)
    {
        _repository = repository;
        _logger = logger;
        _userContext = userContext;
        _timeProvider = timeProvider;
        _slugHelper = slugHelper;
        _random = new Random();
    }

    public async Task IsOwnerAsync(Guid listingId, Guid userId)
    {
        var isOwner = await _repository.IsOwnerAsync(listingId, userId);

        if (!isOwner)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", listingId);
        }
    }

    public async Task<CoownershipListingDto> GetAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await _repository.GetAsync(listingId, ct);

        if (listing == null)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", listingId);
        }

        return listing;
    }

    public async Task<List<CoownershipListingCard>> GetAllByUserAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _repository.GetAllByUserAsync(ownerId, ct);
    }

    public async Task UpsertListingAsync(CoownershipListingDto dto, CancellationToken ct = default)
    {
        if (dto.Version <= 0)
        {
            throw new ArgumentException("Version must be greater than zero.");
        }
        
        var existingListing = await _repository.GetAsync(dto.Id, ct);

        if (existingListing != null)
        {
            if (dto.Version < existingListing.Version)
            {
                _logger.LogInformation(
                    "Skipping update for listing {ListingId}. Incoming version: {IncomingVersion}, Existing version: {ExistingVersion}", 
                    dto.Id, dto.Version, existingListing.Version);
                return;
            }
            
            existingListing.Title = dto.Title;
            existingListing.Description = dto.Description;
            existingListing.CategorySlug = dto.CategorySlug;
            existingListing.City = dto.City;
            existingListing.ImagesUrls = dto.ImagesUrls;
            existingListing.SharePrice = dto.SharePrice;
            existingListing.TotalShares = dto.TotalShares;
            existingListing.AvailableShares = dto.AvailableShares;
            existingListing.CatalogListingId = dto.CatalogListingId;
            existingListing.FundingDeadline = dto.FundingDeadline;
            existingListing.IsActive = dto.IsActive;
            existingListing.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            existingListing.Version = dto.Version;
            
            if (existingListing.Title != dto.Title || string.IsNullOrEmpty(existingListing.TitleSlug))
            {
                existingListing.TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}";
            }

            await _repository.UpdateAsync(existingListing, dto.OwnerId, ct);
        }
        else
        {
            var newListing = new CoownershipListing
            {
                Id = dto.Id,
                TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}",
                Title = dto.Title,
                Description = dto.Description,
                OwnerId = dto.OwnerId,
                CategorySlug = dto.CategorySlug,
                City = dto.City,
                ImagesUrls = dto.ImagesUrls,
                SharePrice = dto.SharePrice,
                TotalShares = dto.TotalShares,
                AvailableShares = dto.TotalShares,
                CatalogListingId = dto.CatalogListingId,
                FundingDeadline = dto.FundingDeadline,
                IsActive = dto.IsActive,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                Version = 0
            };

            if (newListing.OwnerId == Guid.Empty)
            {
                throw new AuthenticationException("User id is empty.");
            }

            await _repository.CreateAsync(newListing, ct);
        }

        await _repository.SaveChangesAsync(ct);
    }


    public async Task RemoveListingAsync(Guid listingId, int? version = null, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? (_userContext.IsAdmin ? null : _userContext.UserId);
        
        var listing = await _repository.GetAsync(listingId, ct);

        if (listing == null)
        {
            _logger.LogInformation("Listing {ListingId} already deleted or does not exist.", listingId);
            return;
        }
        
        if (currentOwnerId != null && listing.OwnerId != currentOwnerId)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", listingId);
        }
        
        if (version.HasValue && version.Value <= listing.Version)
        {
            _logger.LogInformation(
                "Skipping delete for listing {ListingId}. Message version {MessageVersion} is older or equal to current version {CurrentVersion}.", 
                listingId, version.Value, listing.Version);
            return;
        }
        
        var success = await _repository.RemoveAsync(listingId, currentOwnerId, ct);
        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", listingId);
        }

        await _repository.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? (_userContext.IsAdmin ? null : _userContext.UserId);
        await ExecuteStateChangeAsync(listingId, currentOwnerId, false, ct);
    }

    public async Task ActivateAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? (_userContext.IsAdmin ? null : _userContext.UserId);
        await ExecuteStateChangeAsync(listingId, currentOwnerId, true, ct);
    }

    private async Task ExecuteStateChangeAsync(Guid listingId, Guid? ownerId, bool isActive, CancellationToken ct)
    {
        var success = isActive
            ? await _repository.ActivateAsync(listingId, ownerId, ct)
            : await _repository.DeactivateAsync(listingId, ownerId, ct);

        if (!success)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", listingId);
        }

        await _repository.SaveChangesAsync(ct);
    }
}