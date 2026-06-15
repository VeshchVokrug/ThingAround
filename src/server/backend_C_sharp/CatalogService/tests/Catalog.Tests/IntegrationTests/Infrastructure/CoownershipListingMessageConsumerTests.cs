using Application.Services.Abstractions;
using Catalog.Contracts.DTO.Listing.Coownership;
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
    public async Task Consume_WhenActionIsCreate_CallsUpsertService()
    {
        // Arrange
        var message = new CoownershipListingMessage
        {
            Action = CoownershipListingAction.Create,
            ListingId = Guid.NewGuid(),
            Version = 1,
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

        // Act
        await _consumer.Consume(context);

        // Assert
        await _service.Received(1).UpsertListingAsync(
            Arg.Is<CoownershipListingDto>(x =>
                x.Id == message.ListingId
                && x.Version == message.Version
                && x.CatalogListingId == message.CatalogListingId
                && x.TotalShares == message.TotalShares),
            CancellationToken.None);
    }

    [Fact]
    public async Task Consume_WhenActionIsUpdate_CallsUpsertService()
    {
        // Arrange
        var message = new CoownershipListingMessage
        {
            Action = CoownershipListingAction.Update,
            ListingId = Guid.NewGuid(),
            Version = 2,
            OwnerId = Guid.NewGuid(),
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Updated Shared Camera",
            Description = "Updated Description",
            City = "Moscow",
            SharePrice = 1600,
            TotalShares = 8
        };

        var context = Substitute.For<ConsumeContext<CoownershipListingMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _service.Received(1).UpsertListingAsync(
            Arg.Is<CoownershipListingDto>(x =>
                x.Id == message.ListingId
                && x.Version == message.Version
                && x.Title == message.Title),
            CancellationToken.None);
    }

    [Fact]
    public async Task Consume_WhenActionIsDelete_CallsRemoveServiceWithVersion()
    {
        // Arrange
        var message = new CoownershipListingMessage
        {
            Action = CoownershipListingAction.Delete,
            ListingId = Guid.NewGuid(),
            Version = 3,
            OwnerId = Guid.NewGuid()
        };

        var context = Substitute.For<ConsumeContext<CoownershipListingMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _service.Received(1).RemoveListingAsync(
            message.ListingId,
            message.Version,
            message.OwnerId,
            CancellationToken.None);
    }
}