using Application.Exceptions;
using Application.Services;
using Catalog.Contracts.Auth;
using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Core.Contracts;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Slugify;

namespace CatalogService.Tests.IntegrationTests.Application;

public class RentalListingServiceTests
{
    private static readonly SemaphoreSlim FixtureLock = new(1, 1);
    private static PostgresFixture? _fixture;

    private readonly IRentalListingRepository _listingRepo;
    private readonly IAvailabilitySlotRepository _slotRepo;
    private readonly IUserContext _userContext;
    private readonly RentalListingService _service;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ISlugHelper _slugHelper;

    public RentalListingServiceTests()
    {
        _slugHelper = Substitute.For<ISlugHelper>();
        _slugHelper.GenerateSlug(Arg.Any<string>())
            .Returns(call => call.ArgAt<string>(0).ToLowerInvariant().Replace(" ", "-"));

        _listingRepo = Substitute.For<IRentalListingRepository>();
        _slotRepo = Substitute.For<IAvailabilitySlotRepository>();
        _userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<RentalListingService>>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));

        var listingTransaction = Substitute.For<IRepositoryTransaction>();
        listingTransaction.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        listingTransaction.DisposeAsync().Returns(ValueTask.CompletedTask);
        _listingRepo.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(listingTransaction);

        var slotTransaction = Substitute.For<IRepositoryTransaction>();
        slotTransaction.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        slotTransaction.DisposeAsync().Returns(ValueTask.CompletedTask);
        _slotRepo.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(slotTransaction);
        
        _service = new RentalListingService(
            _listingRepo,
            logger,
            _userContext,
            _slotRepo,
            _timeProvider,
            _slugHelper);
    }
    
    [Fact]
    public async Task TryReserveSlots_WhenBookingIdIsNullAndNotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new ReservationSlotsDto { 
            ListingId = Guid.NewGuid(), 
            BookingId = null, 
            Dates = [DateOnly.FromDateTime(DateTime.Now)] 
        };

        _userContext.UserId.Returns(userId);
        _listingRepo.IsOwnerAsync(dto.ListingId, userId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var act = () => _service.TryReserveSlotsAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }

    [Fact]
    public async Task TryReserveSlots_WhenBookingIdIsNotNull_SkipsOwnerCheck()
    {
        // Arrange
        var dto = new ReservationSlotsDto { 
            ListingId = Guid.NewGuid(), 
            BookingId = Guid.NewGuid(), 
            Dates = [DateOnly.FromDateTime(DateTime.Now)] 
        };
    
        _slotRepo.TryReserveSlotsAsync(dto.ListingId, dto.Dates, dto.BookingId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _service.TryReserveSlotsAsync(dto);

        // Assert
        await _listingRepo.DidNotReceive().IsOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _slotRepo.Received(1).TryReserveSlotsAsync(dto.ListingId, dto.Dates, dto.BookingId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RemoveListing_WhenUserIsAdmin_PassesNullAsOwnerId()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _userContext.IsAdmin.Returns(true);
        _listingRepo.RemoveAsync(listingId, null, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _service.RemoveListingAsync(listingId);

        // Assert
        await _listingRepo.Received(1).RemoveAsync(listingId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveListing_WhenUserIsNotAdmin_PassesCurrentUserId()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _userContext.IsAdmin.Returns(false);
        _userContext.UserId.Returns(userId);
    
        _listingRepo.RemoveAsync(listingId, userId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _service.RemoveListingAsync(listingId);

        // Assert
        await _listingRepo.Received(1).RemoveAsync(listingId, userId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task TryReserveSlots_WhenConflictOccurs_ThrowsAvailabilityConflictException()
    {
        // Arrange
        var dto = new ReservationSlotsDto { 
            ListingId = Guid.NewGuid(), 
            BookingId = Guid.NewGuid(), 
            Dates = [DateOnly.FromDateTime(DateTime.Now)] 
        };

        _slotRepo.TryReserveSlotsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<DateOnly>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var act = () => _service.TryReserveSlotsAsync(dto);

        // Assert
        await act.Should().ThrowAsync<AvailabilityConflictException>()
            .Where(e => e.ListingId == dto.ListingId);
    }
    
    [Fact]
    public async Task CancelReservation_WhenBookingIdIsNullAndNotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var slots = new ReservationSlotsDto 
        { 
            ListingId = Guid.NewGuid(), 
            BookingId = null, 
            Dates = [new DateOnly(2024, 10, 10)] 
        };

        _userContext.UserId.Returns(userId);
        _listingRepo.IsOwnerAsync(slots.ListingId, userId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var act = () => _service.CancelReservationAsync(slots);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
        await _slotRepo.DidNotReceive().CancelReservationAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<DateOnly>>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task CancelReservation_WhenBookingIdIsNotNull_CallsRepoDirectly()
    {
        // Arrange
        var slots = new ReservationSlotsDto 
        { 
            ListingId = Guid.NewGuid(), 
            BookingId = Guid.NewGuid(), 
            Dates = [new DateOnly(2024, 10, 10)] 
        };

        // Act
        await _service.CancelReservationAsync(slots);

        // Assert
        await _listingRepo.DidNotReceive().IsOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _slotRepo.Received(1).CancelReservationAsync(slots.ListingId, slots.Dates, slots.BookingId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Deactivate_WhenUserIsOwner_CallsRepoWithUserId()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _userContext.IsAdmin.Returns(false);
        _userContext.UserId.Returns(userId);
        _listingRepo.DeactivateAsync(listingId, userId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.DeactivateAsync(listingId);

        // Assert
        await _listingRepo.Received(1).DeactivateAsync(listingId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_WhenRepoReturnsFalse_ThrowsForbiddenOrNotFound()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _listingRepo.DeactivateAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var act = () => _service.DeactivateAsync(listingId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }
    
    [Fact]
    public async Task UpdateListing_WhenAdminUpdates_PassesNullAsOwnerId()
    {
        // Arrange
        var slotDate = new DateOnly(2026, 4, 10);
        var availabilitySlots = new List<AvailabilitySlotDto>
        {
            new()
            {
                Date = slotDate,
                Version = 1,
                IsAvailable = true,
                IsReversible = true,
                BookingId = null
            }
        };

        var dto = new RentalListingDto
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Title = "New Title",
            CategorySlug = Category.ArtEquip.ToString(),
            DefaultPrice = 100,
            AvailabilitySlots = availabilitySlots
        };

        var currentListing = new RentalListingDto
        {
            Id = dto.Id,
            Version = dto.Version,
            TitleSlug = "new-title",
            Title = dto.Title,
            CategorySlug = dto.CategorySlug,
            DefaultPrice = dto.DefaultPrice,
            AvailabilitySlots = availabilitySlots
        };

        _userContext.IsAdmin.Returns(true);
        _listingRepo.GetAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(currentListing);
        _listingRepo.UpdateAsync(dto, null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.UpdateListingAsync(dto);

        // Assert
        await _listingRepo.Received(1).UpdateAsync(dto, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateListing_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        var slotDate = new DateOnly(2026, 4, 10);
        var availabilitySlots = new List<AvailabilitySlotDto>
        {
            new()
            {
                Date = slotDate,
                Version = 1,
                IsAvailable = true,
                IsReversible = true,
                BookingId = null
            }
        };

        var dto = new RentalListingDto
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Title = "Updated title",
            CategorySlug = Category.ArtEquip.ToString(),
            DefaultPrice = 100,
            AvailabilitySlots = availabilitySlots
        };

        var currentListing = new RentalListingDto
        {
            Id = dto.Id,
            Version = dto.Version,
            TitleSlug = "updated-title",
            Title = dto.Title,
            CategorySlug = dto.CategorySlug,
            DefaultPrice = dto.DefaultPrice,
            AvailabilitySlots = availabilitySlots
        };

        _userContext.IsAdmin.Returns(false);
        _userContext.UserId.Returns(Guid.NewGuid());
        _listingRepo.GetAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(currentListing);
        _listingRepo.UpdateAsync(Arg.Any<RentalListingDto>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = () => _service.UpdateListingAsync(dto);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WhenListingExists_ReturnsListing()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var listing = new RentalListingDto
        {
            Id = listingId,
            CategorySlug = Category.ArtEquip.ToString(),
            Title = "Listing title",
            TitleSlug = "listing-title",
            Description = "Description",
            City = "Moscow",
            DefaultPrice = 100,
            OwnerName = "Owner",
            OwnerPhone = "+70000000000",
            AvailabilitySlots = []
        };

        _listingRepo.GetAsync(listingId, Arg.Any<CancellationToken>())
            .Returns(listing);

        // Act
        var result = await _service.GetAsync(listingId);

        // Assert
        result.Should().BeSameAs(listing);
    }

    [Fact]
    public async Task GetAsync_WhenListingNotFound_ThrowsForbiddenOrNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _listingRepo.GetAsync(listingId, Arg.Any<CancellationToken>())
            .Returns((RentalListingDto?)null);

        // Act
        var act = () => _service.GetAsync(listingId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_WhenStartDateInPast_ThrowsArgumentException()
    {
        // Arrange
        var request = new RentalFilterRequest(StartDate: new DateOnly(2026, 4, 9));

        // Act
        var act = () => _service.GetAllAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        await _listingRepo.DidNotReceive().GetFilteredCatalogAsync(Arg.Any<RentalFilterRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WhenRequestValid_ReturnsPagedResponse()
    {
        // Arrange
        var request = new RentalFilterRequest(
            StartDate: new DateOnly(2026, 4, 10),
            EndDate: new DateOnly(2026, 4, 11));

        var expected = new PagedResponse<RentalListingCard>(
            Items: [new RentalListingCard(Guid.NewGuid(), "Title", "title", null, 100, 4.9f)],
            TotalCount: 1,
            PageNumber: 1,
            PageSize: 12,
            City: null);

        _listingRepo.GetFilteredCatalogAsync(request, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _service.GetAllAsync(request);

        // Assert
        result.Should().BeSameAs(expected);
        await _listingRepo.Received(1).GetFilteredCatalogAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllByUserAsync_WhenCalled_ReturnsCards()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var cards = new List<RentalListingCard>
        {
            new(Guid.NewGuid(), "Title", "title", null, 150, 4.5f)
        };

        _listingRepo.GetAllByUserAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(cards);

        // Act
        var result = await _service.GetAllByUserAsync(ownerId);

        // Assert
        result.Should().BeSameAs(cards);
    }

    [Fact]
    public async Task CreateListingAsync_WhenCalled_SetsOwnerIdFromUserContext_AndReturnsCreatedId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdId = Guid.NewGuid();
        var dto = new CreateRentalListingDto
        {
            Title = "New Title",
            CategorySlug = Category.ArtEquip.ToString(),
            BusyDates = []
        };

        _userContext.UserId.Returns(userId);
        _listingRepo.CreateAsync(
                Arg.Any<RentalListing>(),
                Arg.Any<IEnumerable<DateOnly>>(),
                Arg.Any<CancellationToken>())
            .Returns(createdId);

        // Act
        var result = await _service.CreateListingAsync(dto);

        // Assert
        result.Should().Be(createdId);
        await _listingRepo.Received(1).CreateAsync(
            Arg.Is<RentalListing>(x =>
                x.OwnerId == userId
                && x.Title == dto.Title
                && x.TitleSlug == "new-title"),
            dto.BusyDates,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SystemDeactivateAsync_WhenRepoReturnsTrue_CallsRepoWithNullOwnerId()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _listingRepo.DeactivateAsync(listingId, null, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _service.SystemDeactivateAsync(listingId);

        // Assert
        await _listingRepo.Received(1).DeactivateAsync(listingId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SystemDeactivateAsync_WhenRepoReturnsFalse_ThrowsForbiddenOrNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _listingRepo.DeactivateAsync(listingId, null, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var act = () => _service.SystemDeactivateAsync(listingId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenOrNotFoundException>();
    }
    [Fact]
    public async Task UpdateListing_AllDatesOpen_CloseSeveral_ShouldCloseOnlyTargetDates()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var listingId = await SeedListingWithSlotsAsync(context, ownerId, closedDates: []);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var closeDates = snapshot.AvailabilitySlots.Take(3).Select(x => x.Date).ToHashSet();

        var updateDto = BuildUpdateDto(snapshot, snapshot.AvailabilitySlots
            .Select(slot => slot with { IsAvailable = !closeDates.Contains(slot.Date) })
            .ToList());

        await service.UpdateListingAsync(updateDto);

        var updated = await service.GetAsync(listingId);

        updated.AvailabilitySlots.Should().AllSatisfy(slot =>
        {
            if (closeDates.Contains(slot.Date))
            {
                slot.IsAvailable.Should().BeFalse();
            }
            else
            {
                slot.IsAvailable.Should().BeTrue();
            }
        });
    }

    [Fact]
    public async Task UpdateListing_SomeDatesClosed_OpenSeveral_ShouldOpenOnlyTargetDates()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var initiallyClosed = new[] { today.AddDays(2), today.AddDays(3), today.AddDays(4), today.AddDays(7) };

        var listingId = await SeedListingWithSlotsAsync(context, ownerId, initiallyClosed);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var openDates = initiallyClosed.Take(2).ToHashSet();
        var stillClosed = initiallyClosed.Skip(2).ToHashSet();

        var updateDto = BuildUpdateDto(snapshot, snapshot.AvailabilitySlots
            .Select(slot => openDates.Contains(slot.Date)
                ? slot with { IsAvailable = true }
                : slot)
            .ToList());

        await service.UpdateListingAsync(updateDto);

        var updated = await service.GetAsync(listingId);

        updated.AvailabilitySlots.Should().AllSatisfy(slot =>
        {
            if (openDates.Contains(slot.Date))
            {
                slot.IsAvailable.Should().BeTrue();
            }
            else if (stillClosed.Contains(slot.Date))
            {
                slot.IsAvailable.Should().BeFalse();
            }
        });
    }

    [Fact]
    public async Task UpdateListing_SomeDatesClosedAndOpen_OpenSomeAndCloseSome_ShouldMatchSnapshot()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var initiallyClosed = new[] { today.AddDays(2), today.AddDays(3), today.AddDays(4) };

        var listingId = await SeedListingWithSlotsAsync(context, ownerId, initiallyClosed);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var openDates = initiallyClosed.Take(2).ToHashSet();
        var closeDates = snapshot.AvailabilitySlots
            .Where(x => x.IsAvailable)
            .Select(x => x.Date)
            .Take(2)
            .ToHashSet();

        var requestedSlots = snapshot.AvailabilitySlots
            .Select(slot =>
            {
                if (openDates.Contains(slot.Date))
                {
                    return slot with { IsAvailable = true };
                }

                if (closeDates.Contains(slot.Date))
                {
                    return slot with { IsAvailable = false };
                }

                return slot;
            })
            .ToList();

        var updateDto = BuildUpdateDto(snapshot, requestedSlots);

        await service.UpdateListingAsync(updateDto);

        var updated = await service.GetAsync(listingId);
        var expectedByDate = requestedSlots.ToDictionary(x => x.Date, x => x.IsAvailable);

        updated.AvailabilitySlots.Should().HaveCount(expectedByDate.Count);
        updated.AvailabilitySlots.Should().AllSatisfy(slot =>
            slot.IsAvailable.Should().Be(expectedByDate[slot.Date]));
    }

    [Fact]
    public async Task UpdateListing_WhenListingVersionConflict_ShouldThrowOptimisticConcurrencyException()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var listingId = await SeedListingWithSlotsAsync(context, ownerId, closedDates: []);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var updateDto = BuildUpdateDto(snapshot, snapshot.AvailabilitySlots);
        updateDto = updateDto with { Version = snapshot.Version + 1 };

        var act = () => service.UpdateListingAsync(updateDto);

        await act.Should().ThrowAsync<OptimisticConcurrencyException>();
    }

    [Fact]
    public async Task UpdateListing_WhenSlotVersionConflict_ShouldThrowOptimisticConcurrencyException()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var listingId = await SeedListingWithSlotsAsync(context, ownerId, closedDates: []);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var requestedSlots = snapshot.AvailabilitySlots
            .Select((slot, i) => i == 0
                ? slot with { Version = slot.Version + 1 }
                : slot)
            .ToList();

        var updateDto = BuildUpdateDto(snapshot, requestedSlots);
        var act = () => service.UpdateListingAsync(updateDto);

        await act.Should().ThrowAsync<OptimisticConcurrencyException>();
    }

    [Fact]
    public async Task UpdateListing_WhenTryingToOpenClientBookedSlot_ShouldThrowAvailabilityConflictException()
    {
        var fixture = await GetFixtureAsync();
        await using var context = fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var bookingDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime).AddDays(2);
        var listingId = await SeedListingWithSlotsAsync(context, ownerId, closedDates: [], clientBookingDates: [bookingDate]);
        var service = CreateService(context, ownerId);

        var snapshot = await service.GetAsync(listingId);
        var requestedSlots = snapshot.AvailabilitySlots
            .Select(slot => slot.Date == bookingDate
                ? slot with { IsAvailable = true }
                : slot)
            .ToList();

        var updateDto = BuildUpdateDto(snapshot, requestedSlots);
        var act = () => service.UpdateListingAsync(updateDto);

        await act.Should().ThrowAsync<AvailabilityConflictException>();
    }

    private RentalListingService CreateService(CatalogDbContext context, Guid userId)
    {
        var listingRepository = new RentalListingRepository(context, new AvailabilitySlotRepository(context, _timeProvider));
        var slotRepository = new AvailabilitySlotRepository(context, _timeProvider);
        var logger = Substitute.For<ILogger<RentalListingService>>();
        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(userId);
        userContext.IsAdmin.Returns(false);

        return new RentalListingService(
            listingRepository,
            logger,
            userContext,
            slotRepository,
            _timeProvider,
            _slugHelper);
    }

    private static RentalListingDto BuildUpdateDto(RentalListingDto snapshot, List<AvailabilitySlotDto> requestedSlots)
    {
        return new RentalListingDto
        {
            Id = snapshot.Id,
            Version = snapshot.Version,
            CategorySlug = snapshot.CategorySlug,
            TitleSlug = snapshot.TitleSlug,
            Title = snapshot.Title,
            Description = snapshot.Description,
            ImagesUrls = snapshot.ImagesUrls,
            City = snapshot.City,
            DefaultPrice = snapshot.DefaultPrice,
            OwnerId = snapshot.OwnerId,
            OwnerRating = snapshot.OwnerRating,
            OwnerName = snapshot.OwnerName,
            OwnerPhone = snapshot.OwnerPhone,
            OwnerSocialsUrls = snapshot.OwnerSocialsUrls,
            AvailabilitySlots = requestedSlots
        };
    }

    private async Task<Guid> SeedListingWithSlotsAsync(
        CatalogDbContext context,
        Guid ownerId,
        IReadOnlyCollection<DateOnly> closedDates,
        IReadOnlyCollection<DateOnly>? clientBookingDates = null)
    {
        var listingId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        clientBookingDates ??= [];

        var listing = new RentalListing
        {
            Id = listingId,
            Version = 1,
            OwnerId = ownerId,
            TitleSlug = $"listing-{listingId:N}",
            CategorySlug = Category.Camping.ToString(),
            Title = "Test listing",
            Description = "Test description",
            ImagesUrls = ["image-1.jpg"],
            City = "Moscow",
            DefaultPrice = 1000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Contact = new ContactInfo
            {
                ManagerId = ownerId,
                PersonName = "Owner",
                PersonPhone = "79990000000",
                SocialsUrls = null
            }
        };

        await context.RentalListings.AddAsync(listing);

        var slots = Enumerable.Range(0, 10)
            .Select(offset =>
            {
                var date = today.AddDays(offset + 1);
                var isClientBooked = clientBookingDates.Contains(date);
                var isClosedByOwner = closedDates.Contains(date);
                return new AvailabilitySlot
                {
                    ListingId = listingId,
                    Date = date,
                    Version = 1,
                    IsAvailable = !isClientBooked && !isClosedByOwner,
                    BookingId = isClientBooked ? Guid.NewGuid() : null,
                    ReservedAt = isClientBooked || isClosedByOwner ? DateTime.UtcNow : null,
                    Price = 1000
                };
            })
            .ToList();

        await context.AvailabilitySlots.AddRangeAsync(slots);
        await context.SaveChangesAsync();

        return listingId;
    }

    private static async Task ResetDatabaseAsync(CatalogDbContext context)
    {
        await context.AvailabilitySlots.ExecuteDeleteAsync();
        await context.RentalListings.ExecuteDeleteAsync();
    }

    private static async Task<PostgresFixture> GetFixtureAsync()
    {
        if (_fixture != null)
        {
            return _fixture;
        }

        await FixtureLock.WaitAsync();
        try
        {
            if (_fixture == null)
            {
                var fixture = new PostgresFixture();
                await fixture.InitializeAsync();
                _fixture = fixture;
            }

            return _fixture;
        }
        finally
        {
            FixtureLock.Release();
        }
    }
}
