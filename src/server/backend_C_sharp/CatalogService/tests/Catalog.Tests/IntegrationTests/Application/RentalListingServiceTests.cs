using Application.Exceptions;
using Application.Services;
using Catalog.Contracts.Auth;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;
using Catalog.Contracts.Repository.Abstractions;
using Core.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CatalogService.Tests.IntegrationTests.Application;

public class RentalListingServiceTests
{
    private readonly IRentalListingRepository _listingRepo;
    private readonly IAvailabilitySlotRepository _slotRepo;
    private readonly IUserContext _userContext;
    private readonly RentalListingService _service;

    public RentalListingServiceTests()
    {
        _listingRepo = Substitute.For<IRentalListingRepository>();
        _slotRepo = Substitute.For<IAvailabilitySlotRepository>();
        _userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<RentalListingService>>();

        _service = new RentalListingService(
            _listingRepo,
            logger,
            _userContext,
            _slotRepo);
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
        var dto = new UpdateRentalListingDto { Id = Guid.NewGuid(), Title = "New Title", CategorySlug = Category.ArtEquip.ToString() };
        _userContext.IsAdmin.Returns(true);
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
        var dto = new UpdateRentalListingDto { Id = Guid.NewGuid(), CategorySlug = Category.ArtEquip.ToString() };
        _userContext.IsAdmin.Returns(false);
        _userContext.UserId.Returns(Guid.NewGuid());
        _listingRepo.UpdateAsync(Arg.Any<UpdateRentalListingDto>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = () => _service.UpdateListingAsync(dto);

        // Assert
        await act.Should().NotThrowAsync();
    }
}