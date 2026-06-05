using Application.Exceptions;
using Application.Services;
using Catalog.Contracts.DTO.Listing.Coownership;
using Catalog.Contracts.Repository.Abstractions;
using Core.Auth;
using Domain.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Slugify;

namespace CatalogService.Tests.IntegrationTests.Application;

public class CoownershipListingServiceTests
{
    private readonly ICoownershipListingRepository _repository;
    private readonly IUserContext _userContext;
    private readonly CoownershipListingService _service;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ISlugHelper _slugHelper;

    public CoownershipListingServiceTests()
    {
        _repository = Substitute.For<ICoownershipListingRepository>();
        _userContext = Substitute.For<IUserContext>();
        _slugHelper = Substitute.For<ISlugHelper>();
        _slugHelper.GenerateSlug(Arg.Any<string>()).Returns(call => call.ArgAt<string>(0).ToLowerInvariant().Replace(" ", "-"));
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));

        _service = new CoownershipListingService(
            _repository,
            Substitute.For<ILogger<CoownershipListingService>>(),
            _userContext,
            _timeProvider,
            _slugHelper);
    }

    [Fact]
    public async Task CreateListingAsync_WhenUserContextIsSet_UsesCurrentUserAndReturnsCreatedId()
    {
        var userId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var dto = new CreateCoownershipListingDto
        {
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared Camera",
            Description = "Camera",
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 8,
            FundingDeadline = new DateOnly(2026, 7, 1)
        };

        _userContext.UserId.Returns(userId);
        _repository.CreateAsync(Arg.Any<CoownershipListing>(), Arg.Any<CancellationToken>()).Returns(createdId);

        var result = await _service.CreateListingAsync(dto);

        result.Should().Be(createdId);
        await _repository.Received(1).CreateAsync(
            Arg.Is<CoownershipListing>(x =>
                x.OwnerId == userId
                && x.TotalShares == dto.TotalShares
                && x.AvailableShares == dto.TotalShares
                && x.CatalogListingId == dto.CatalogListingId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateListingAsync_WhenOwnerIdProvided_UsesProvidedOwnerId()
    {
        var ownerId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var dto = new CreateCoownershipListingDto
        {
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared Camera",
            Description = "Camera",
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 8
        };

        _repository.CreateAsync(Arg.Any<CoownershipListing>(), Arg.Any<CancellationToken>()).Returns(createdId);

        var result = await _service.CreateListingAsync(dto, ownerId);

        result.Should().Be(createdId);
        await _repository.Received(1).CreateAsync(
            Arg.Is<CoownershipListing>(x => x.OwnerId == ownerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveListingAsync_WhenRepoReturnsFalse_ThrowsForbiddenOrNotFoundException()
    {
        var listingId = Guid.NewGuid();

        _repository.RemoveAsync(listingId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _service.RemoveListingAsync(listingId, Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }

    [Fact]
    public async Task UpdateListingAsync_WhenRepoSucceeds_CallsUpdateWithProvidedOwner()
    {
        var ownerId = Guid.NewGuid();
        var dto = new CoownershipListingDto
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Title = "Shared Camera",
            TitleSlug = "old-slug",
            CategorySlug = "electronics",
            Description = "Desc",
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 8,
            AvailableShares = 8,
            CatalogListingId = Guid.NewGuid(),
            IsActive = true,
            OwnerId = ownerId
        };

        _repository.UpdateAsync(Arg.Any<CoownershipListingDto>(), ownerId, Arg.Any<CancellationToken>()).Returns(true);

        await _service.UpdateListingAsync(dto, ownerId);

        await _repository.Received(1).UpdateAsync(
            Arg.Is<CoownershipListingDto>(x => x.TitleSlug.StartsWith("shared-camera-")),
            ownerId,
            Arg.Any<CancellationToken>());
    }
}