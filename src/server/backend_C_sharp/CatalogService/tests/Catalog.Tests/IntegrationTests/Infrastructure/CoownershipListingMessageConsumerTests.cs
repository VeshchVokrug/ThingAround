using Application.Services.Abstractions;
using Core.Events;
using FluentAssertions;
using Infrastructure.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CatalogService.Tests.IntegrationTests.Infrastructure;

public class CoownershipListingMessageConsumerTests
{
    private readonly ICoownershipListingService _service;
    private readonly CoownershipListingMessageConsumer _consumer;

    public CoownershipListingMessageConsumerTests()
    {
        _service = Substitute.For<ICoownershipListingService>();
        _consumer = new CoownershipListingMessageConsumer(
            Substitute.For<ILogger<CoownershipListingMessageConsumer>>(),
            _service);
    }

    [Fact]
    public async Task Consume_WhenActionIsCreate_CallsCreateService()
    {
        var message = new CoownershipListingMessage
        {
            Action = CoownershipListingAction.Create,
            OwnerId = Guid.NewGuid(),
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared Camera",
            Description = "Camera",
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 8
        };

        var context = Substitute.For<ConsumeContext<CoownershipListingMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await _consumer.Consume(context);

        await _service.Received(1).CreateListingAsync(
            Arg.Is<Catalog.Contracts.DTO.Listing.Coownership.CreateCoownershipListingDto>(x =>
                x.CatalogListingId == message.CatalogListingId
                && x.TotalShares == message.TotalShares),
            message.OwnerId,
            CancellationToken.None);
    }

    [Fact]
    public async Task Consume_WhenActionIsDelete_CallsRemoveService()
    {
        var message = new CoownershipListingMessage
        {
            Action = CoownershipListingAction.Delete,
            ListingId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<CoownershipListingMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        await _consumer.Consume(context);

        await _service.Received(1).RemoveListingAsync(message.ListingId, message.OwnerId, CancellationToken.None);
    }
}