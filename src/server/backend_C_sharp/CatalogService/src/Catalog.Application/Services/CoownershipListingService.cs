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

    public async Task<Guid> CreateListingAsync(CreateCoownershipListingDto dto, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? _userContext.UserId;
        if (currentOwnerId == Guid.Empty)
        {
            throw new AuthenticationException("User id is empty.");
        }

        var listing = new CoownershipListing
        {
            TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}",
            Title = dto.Title,
            Description = dto.Description,
            OwnerId = currentOwnerId,
            CategorySlug = dto.CategorySlug,
            City = dto.City,
            ImagesUrls = dto.ImagesUrls,
            SharePrice = dto.SharePrice,
            TotalShares = dto.TotalShares,
            AvailableShares = dto.TotalShares,
            CatalogListingId = dto.CatalogListingId,
            FundingDeadline = dto.FundingDeadline,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        var listingId = await _repository.CreateAsync(listing, ct);
        await _repository.SaveChangesAsync(ct);

        return listingId;
    }

    public async Task RemoveListingAsync(Guid listingId, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? (_userContext.IsAdmin ? null : _userContext.UserId);

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

    public async Task UpdateListingAsync(CoownershipListingDto dto, Guid? ownerId = null, CancellationToken ct = default)
    {
        var currentOwnerId = ownerId ?? (_userContext.IsAdmin ? null : _userContext.UserId);
        dto.TitleSlug = $"{_slugHelper.GenerateSlug(dto.Title)}-{_random.Next()}";

        if (dto.Version <= 0)
        {
            throw new ArgumentException("Version must be greater than zero.");
        }

        var updated = await _repository.UpdateAsync(dto, currentOwnerId, ct);
        if (!updated)
        {
            throw new ForbiddenOrNotFoundException("Объявление совладения", dto.Id);
        }

        await _repository.SaveChangesAsync(ct);
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