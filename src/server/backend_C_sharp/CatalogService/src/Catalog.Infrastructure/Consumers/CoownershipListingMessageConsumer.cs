using Application.Services.Abstractions;
using Catalog.Contracts.DTO.Listing.Coownership;
using Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Consumers;

public class CoownershipListingMessageConsumer : IConsumer<CoownershipListingMessage>
{
    private readonly ILogger<CoownershipListingMessageConsumer> _logger;
    private readonly ICoownershipListingService _service;

    public CoownershipListingMessageConsumer(
        ILogger<CoownershipListingMessageConsumer> logger,
        ICoownershipListingService service)
    {
        _logger = logger;
        _service = service;
    }

    public async Task Consume(ConsumeContext<CoownershipListingMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing coownership listing message {Action} for {ListingId}", message.Action, message.ListingId);

        switch (message.Action)
        {
            case CoownershipListingAction.Create:
                await _service.CreateListingAsync(message.ToCreateDto(), message.OwnerId, context.CancellationToken);
                return;
            case CoownershipListingAction.Update:
                await _service.UpdateListingAsync(message.ToDto(), message.OwnerId, context.CancellationToken);
                return;
            case CoownershipListingAction.Delete:
                await _service.RemoveListingAsync(message.ListingId, message.OwnerId, context.CancellationToken);
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

internal static class CoownershipListingMessageMapper
{
    public static CreateCoownershipListingDto ToCreateDto(this CoownershipListingMessage message)
    {
        return new CreateCoownershipListingDto
        {
            CatalogListingId = message.CatalogListingId,
            CategorySlug = message.CategorySlug,
            Title = message.Title,
            Description = message.Description,
            ImagesUrls = message.ImagesUrls,
            City = message.City,
            SharePrice = message.SharePrice,
            TotalShares = message.TotalShares,
            FundingDeadline = message.FundingDeadline
        };
    }

    public static CoownershipListingDto ToDto(this CoownershipListingMessage message)
    {
        return new CoownershipListingDto
        {
            Id = message.ListingId,
            Version = message.Version,
            TitleSlug = message.TitleSlug,
            CategorySlug = message.CategorySlug,
            Title = message.Title,
            Description = message.Description,
            ImagesUrls = message.ImagesUrls,
            City = message.City,
            SharePrice = message.SharePrice,
            TotalShares = message.TotalShares,
            AvailableShares = message.AvailableShares,
            CatalogListingId = message.CatalogListingId,
            FundingDeadline = message.FundingDeadline,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            IsActive = message.IsActive,
            OwnerId = message.OwnerId
        };
    }
}