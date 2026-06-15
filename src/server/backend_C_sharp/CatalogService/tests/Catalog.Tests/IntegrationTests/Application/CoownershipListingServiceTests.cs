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
    public async Task GetAsync_WhenListingExists_ReturnsMappedDto()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            Version = 1,
            Title = "Shared Camera",
            CategorySlug = "electronics",
            OwnerId = ownerId,
            IsActive = true
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);

        // Act
        var result = await _service.GetAsync(listingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(listingId);
        result.Title.Should().Be("Shared Camera");
        result.CategorySlug.Should().Be("electronics");
    }

    [Fact]
    public async Task GetAsync_WhenListingDoesNotExist_ThrowsForbiddenOrNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns((CoownershipListingDto?)null);

        // Act
        var act = () => _service.GetAsync(listingId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }

    [Fact]
    public async Task UpsertListingAsync_WhenListingDoesNotExist_CreatesNewListing()
    {
        // Arrange
        var dto = new CoownershipListingDto
        {
            Id = Guid.NewGuid(),
            Version = 1,
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared Camera",
            Description = "Camera",
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 8,
            AvailableShares = 8,
            FundingDeadline = new DateOnly(2026, 7, 1),
            OwnerId = Guid.NewGuid(),
            IsActive = true
        };

        _repository.GetAsync(dto.Id, Arg.Any<CancellationToken>()).Returns((CoownershipListingDto?)null);

        // Act
        await _service.UpsertListingAsync(dto);

        // Assert
        await _repository.Received(1).CreateAsync(
            Arg.Is<CoownershipListing>(x =>
                x.Id == dto.Id
                && x.OwnerId == dto.OwnerId
                && x.TotalShares == dto.TotalShares
                && x.CategorySlug == dto.CategorySlug
                && x.Version == dto.Version),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertListingAsync_WhenListingExistsWithLowerVersion_UpdatesListingAndSaves()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            Version = 1, // Текущая версия в БД меньше
            Title = "Old Title",
            CategorySlug = "electronics",
            OwnerId = ownerId,
            IsActive = true
        };

        var dto = new CoownershipListingDto
        {
            Id = listingId,
            Version = 2, // Новая версия больше
            Title = "New Title",
            CategorySlug = "electronics",
            OwnerId = ownerId,
            IsActive = true
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);
        _repository.UpdateAsync(Arg.Any<CoownershipListingDto>(), ownerId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.UpsertListingAsync(dto);

        // Assert
        await _repository.Received(1).UpdateAsync(
            Arg.Is<CoownershipListingDto>(x =>
                x.Id == listingId
                && x.Version == 2
                && x.Title == "New Title"),
            ownerId,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertListingAsync_WhenListingExistsWithEqualOrHigherVersion_SkipsExecution()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            Version = 2,
            Title = "Current Title",
            CategorySlug = "electronics",
            OwnerId = ownerId
        };

        var dto = new CoownershipListingDto
        {
            Id = listingId,
            Version = 2, // Равная версия
            Title = "Outdated Update Title",
            CategorySlug = "electronics",
            OwnerId = ownerId
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);

        // Act
        await _service.UpsertListingAsync(dto);

        // Assert
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<CoownershipListingDto>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveListingAsync_WhenListingDoesNotExist_ReturnsGracefullyWithoutThrowing()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns((CoownershipListingDto?)null);

        // Act
        var act = () => _service.RemoveListingAsync(listingId, version: 1);

        // Assert
        await act.Should().NotThrowAsync();
        await _repository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveListingAsync_WhenOwnerMismatchAndNotAdmin_ThrowsForbiddenOrNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        
        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            OwnerId = ownerId,
            Version = 1,
            CategorySlug = "electronics"
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);
        _userContext.UserId.Returns(currentUserId);
        _userContext.IsAdmin.Returns(false);

        // Act
        var act = () => _service.RemoveListingAsync(listingId, version: 2);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }

    [Fact]
    public async Task RemoveListingAsync_WhenVersionIsOlderOrEqual_SkipsDeletion()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            OwnerId = ownerId,
            Version = 3,
            CategorySlug = "electronics"
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);

        // Act
        await _service.RemoveListingAsync(listingId, version: 2, ownerId: ownerId);

        // Assert
        await _repository.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveListingAsync_WhenValidParamsAndNewerVersion_PerformsDeletion()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingDto = new CoownershipListingDto
        {
            Id = listingId,
            OwnerId = ownerId,
            Version = 1,
            CategorySlug = "electronics"
        };

        _repository.GetAsync(listingId, Arg.Any<CancellationToken>()).Returns(existingDto);
        _repository.RemoveAsync(listingId, ownerId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.RemoveListingAsync(listingId, version: 2, ownerId: ownerId);

        // Assert
        await _repository.Received(1).RemoveAsync(listingId, ownerId, Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}