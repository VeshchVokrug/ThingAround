using Application.Services;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace CatalogService.Tests.IntegrationTests.Application;


public class UpdateSlotsUseCaseTests
{
    private readonly IAvailabilitySlotRepository _slotRepo;
    private readonly IListingQueryRepository _listingQueryRepo;
    private readonly FakeTimeProvider _timeProvider;
    private readonly UpdateSlotsUseCase _useCase;

    public UpdateSlotsUseCaseTests()
    {
        _slotRepo = Substitute.For<IAvailabilitySlotRepository>();
        _listingQueryRepo = Substitute.For<IListingQueryRepository>();
        
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));

        _useCase = new UpdateSlotsUseCase(
            _slotRepo,
            _listingQueryRepo,
            _timeProvider);
    }
    

    [Fact]
    public async Task RemoveExpiredAndCreateNewSlotsAsync_WhenExpiredSlotsExist_RemovesOnlyPastSlots()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var defaultPrice = 100;
        var listingPrices = new List<ListingPrice>
        {
            new(listingId, defaultPrice)
        };

        var earliestDate = new DateOnly(2026, 4, 8);
        var latestDate = new DateOnly(2026, 6, 10); 

        _listingQueryRepo.GetAllRentalListingPrices().Returns(listingPrices);
        _listingQueryRepo.GetEarliestSlotDate(listingId).Returns(earliestDate);
        _listingQueryRepo.GetLatestSlotDate(listingId).Returns(latestDate);

        // Act
        await _useCase.RemoveExpiredAndCreateNewSlotsAsync();

        // Assert
        var expectedDeletedDates = new List<DateOnly> { new(2026, 4, 8), new(2026, 4, 9) };

        await _slotRepo.Received(1).RemoveRangeAsync(
            listingId,
            Arg.Is<IEnumerable<DateOnly>>(dates => dates.SequenceEqual(expectedDeletedDates)));

        await _slotRepo.DidNotReceive().CreateRangeAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<IEnumerable<DateOnly>>());
    }

    [Fact]
    public async Task RemoveExpiredAndCreateNewSlotsAsync_WhenFutureSlotsAreMissing_CreatesOnlyMissingSlotsAtTheEnd()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var defaultPrice = 100;
        var listingPrices = new List<ListingPrice>
        {
            new(listingId, defaultPrice)
        };

        var earliestDate = new DateOnly(2026, 4, 10);
        var latestDate = new DateOnly(2026, 6, 8); 

        _listingQueryRepo.GetAllRentalListingPrices().Returns(listingPrices);
        _listingQueryRepo.GetEarliestSlotDate(listingId).Returns(earliestDate);
        _listingQueryRepo.GetLatestSlotDate(listingId).Returns(latestDate);

        // Act
        await _useCase.RemoveExpiredAndCreateNewSlotsAsync();

        // Assert
        var expectedCreatedDates = new List<DateOnly> { new(2026, 6, 9), new(2026, 6, 10) };

        await _slotRepo.Received(1).CreateRangeAsync(
            listingId,
            defaultPrice,
            Arg.Is<IEnumerable<DateOnly>>(dates => dates.SequenceEqual(expectedCreatedDates)));

        await _slotRepo.DidNotReceive().RemoveRangeAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<DateOnly>>());
    }

    [Fact]
    public async Task RemoveExpiredAndCreateNewSlotsAsync_WhenSlotsAreFullyUpToDate_DoesNoDatabaseActions()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var defaultPrice = 100;
        var listingPrices = new List<ListingPrice>
        {
            new(listingId, defaultPrice)
        };

        var earliestDate = new DateOnly(2026, 4, 10);
        var latestDate = new DateOnly(2026, 6, 10);

        _listingQueryRepo.GetAllRentalListingPrices().Returns(listingPrices);
        _listingQueryRepo.GetEarliestSlotDate(listingId).Returns(earliestDate);
        _listingQueryRepo.GetLatestSlotDate(listingId).Returns(latestDate);

        // Act
        await _useCase.RemoveExpiredAndCreateNewSlotsAsync();

        // Assert
        await _slotRepo.DidNotReceive().RemoveRangeAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<DateOnly>>());
        await _slotRepo.DidNotReceive().CreateRangeAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<IEnumerable<DateOnly>>());
    }
}