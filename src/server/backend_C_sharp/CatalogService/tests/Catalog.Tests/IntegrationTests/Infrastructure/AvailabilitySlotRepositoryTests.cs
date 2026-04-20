using Catalog.Contracts.DTO.AvailableSlot;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CatalogService.Tests.IntegrationTests.Infrastructure;

[Collection("PostgresCollection")]
public class AvailabilitySlotRepositoryTests
{
    private readonly PostgresFixture _fixture;
    private readonly FakeTimeProvider _timeProvider;

    public AvailabilitySlotRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));
    }
    
    private AvailabilitySlotRepository CreateRepository(CatalogDbContext context) 
        => new AvailabilitySlotRepository(context, _timeProvider);
    
    [Fact]
    public async Task CreateAvailabilitySlotAsync_ValidData_ShouldSaveSlotToDatabase()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 1);

        // Act
        await sut.CreateAvailabilitySlotAsync(listingId, 500, date);

        // Assert
        var slot = await context.AvailabilitySlots.FirstOrDefaultAsync(s => s.ListingId == listingId);
        slot.Should().NotBeNull();
        slot.Price.Should().Be(500);
        slot.Date.Should().Be(date);
    }

    [Fact]
    public async Task CreateInitialSlotsAsync_WithBusyDates_ShouldOnlyCreateFreeSlotsInFuture()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
    
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);
        var busyDate = today.AddDays(2);
    
        var busyDates = new List<DateOnly> { busyDate };

        // Act
        await sut.CreateInitialSlotsAsync(listingId, 100, busyDates, CancellationToken.None);

        // Assert
        var slots = await context.AvailabilitySlots
            .Where(x => x.ListingId == listingId)
            .ToListAsync();

        slots.Where(s => busyDates.Contains(s.Date))
            .Should().AllSatisfy(x => x.IsAvailable.Should().BeFalse());
        slots.Where(s => !busyDates.Contains(s.Date))
            .Should().AllSatisfy(s => s.IsAvailable.Should().BeTrue());
        slots.Should().AllSatisfy(x => x.Date.Should().BeOnOrAfter(today));
        slots.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task GetAvailabilitySlotsAsync_ShouldReturnOnlyAvailableSlotsWithin60Days()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

        var validDate = today.AddDays(10);
        var tooFarDate = today.AddDays(61);

        context.AvailabilitySlots.AddRange(
            new AvailabilitySlot { ListingId = listingId, Date = validDate, Price = 100, IsAvailable = true },
            new AvailabilitySlot { ListingId = listingId, Date = today, Price = 100, IsAvailable = false },
            new AvailabilitySlot { ListingId = listingId, Date = tooFarDate, Price = 100, IsAvailable = true }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAvailabilitySlotsAsync(listingId);

        // Assert
        var availabilitySlotDtos = result as List<AvailableSlotDto> ?? result.ToList();
        availabilitySlotDtos.Should().ContainSingle();
        availabilitySlotDtos.First().Date.Should().Be(validDate);
    }
    
    [Fact]
    public async Task TryReserveSlotsAsync_OwnerClosesDates_ShouldBeCorrectAndIdempotent()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        Guid? bookingId = null;
        var date = new DateOnly(2026, 4, 15);
        
        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, 
            Date = date, 
            IsAvailable = true, 
            Price = 100 
        });
        await context.SaveChangesAsync();

        var firstCallTime = _timeProvider.GetUtcNow().UtcDateTime;
        
        // Act
        var firstResult = await sut.TryReserveSlotsAsync(listingId, [date], bookingId);
        
        firstResult.Should().BeTrue();
        
        using (var context1 = _fixture.CreateContext())
        {
            var slot = await context1.AvailabilitySlots.FirstAsync(s => s.ListingId == listingId);
            slot.IsAvailable.Should().BeFalse();
            slot.BookingId.Should().BeNull();
            slot.ReservedAt.Should().BeCloseTo(firstCallTime, TimeSpan.FromSeconds(1));
            
            var initialReservedAt = slot.ReservedAt;
            
            _timeProvider.Advance(TimeSpan.FromMinutes(5));
            
            var secondResult = await sut.TryReserveSlotsAsync(listingId, [date], bookingId);

            // Assert
            secondResult.Should().BeTrue(); 

            using (var context2 = _fixture.CreateContext())
            {
                var secondSlot = await context2.AvailabilitySlots.FirstAsync(s => s.ListingId == listingId);
                
                secondSlot.IsAvailable.Should().BeFalse();
                secondSlot.BookingId.Should().BeNull();
                
                secondSlot.ReservedAt.Should().Be(initialReservedAt);
            }
        }
    }
    
    [Fact]
    public async Task TryReserveSlotsAsync_AllDatesAvailable_ShouldUpdateSlotsAndReturnTrue()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var date = new DateOnly(2026, 4, 15);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, Date = date, IsAvailable = true, Price = 100 
        });
        await context.SaveChangesAsync();

        // Act
        var result = await sut.TryReserveSlotsAsync(listingId, [date], bookingId);

        // Assert
        result.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        var slot = await assertContext.AvailabilitySlots.FirstAsync(s => s.ListingId == listingId);
    
        slot.IsAvailable.Should().BeFalse();
        slot.BookingId.Should().Be(bookingId);
        slot.ReservedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public async Task CancelReservationsAsync_AsOwner_ShouldCancelSlot()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 4, 20);

        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, Date = date, IsAvailable = false
        });
        await context.SaveChangesAsync();

        // Act
        await sut.CancelReservationAsync(listingId, [date]);

        // Assert
        using var assertContext = _fixture.CreateContext();
        var slot = await assertContext.AvailabilitySlots
            .FirstAsync(s => s.ListingId == listingId);
        slot.IsAvailable.Should().BeTrue();
    }
    
    [Fact]
    public async Task CancelReservationsAsync_AsOwner_ShouldNotCancelClientBooking()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 4, 20);
        var clientBookingId = Guid.NewGuid();

        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, Date = date, IsAvailable = false, BookingId = clientBookingId 
        });
        await context.SaveChangesAsync();

        // Act
        await sut.CancelReservationAsync(listingId, [date]);

        // Assert
        using var assertContext = _fixture.CreateContext();
        var slot = await assertContext.AvailabilitySlots.FirstAsync();
        slot.IsAvailable.Should().BeFalse("потому что владелец не может отменить активную бронь клиента этим методом");
    }
    
    [Fact]
    public async Task CancelReservationsAsync_ForBooking_ShouldOnlyCancelSpecificBooking()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var targetBookingId = Guid.NewGuid();
        var otherBookingId = Guid.NewGuid();
        var date1 = new DateOnly(2026, 4, 21);
        var date2 = new DateOnly(2026, 4, 22);

        context.AvailabilitySlots.AddRange(
            new AvailabilitySlot { ListingId = listingId, Date = date1, IsAvailable = false, BookingId = targetBookingId },
            new AvailabilitySlot { ListingId = listingId, Date = date2, IsAvailable = false, BookingId = otherBookingId }
        );
        await context.SaveChangesAsync();

        // Act
        await sut.CancelReservationAsync(listingId, [date1, date2], targetBookingId);

        // Assert
        using var assertContext = _fixture.CreateContext();
        var slots = await assertContext.AvailabilitySlots
            .Where(s => s.ListingId == listingId)
            .OrderBy(s => s.Date).ToListAsync();
    
        slots[0].IsAvailable.Should().BeTrue();
        slots[1].IsAvailable.Should().BeFalse();
    }
    
    [Fact]
    public async Task UpdateSlotPriceAsync_SlotExists_ShouldUpdatePrice()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 1);
    
        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, 
            Date = date, 
            Price = 50 
        });
        await context.SaveChangesAsync();

        // Act
        var result = await sut.UpdateSlotPriceAsync(listingId, date, 150);

        // Assert
        result.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        
        var updatedSlot = await assertContext.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.ListingId == listingId && s.Date == date);

        updatedSlot.Should().NotBeNull();
        updatedSlot!.Price.Should().Be(150);
    }

    [Fact]
    public async Task RemoveAvailabilitySlotAsync_ShouldReturnDeletedCountAndRemoveFromDb()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 1);
    
        context.AvailabilitySlots.Add(new AvailabilitySlot { ListingId = listingId, Date = date, Price = 50 });
        await context.SaveChangesAsync();

        // Act
        var count = await sut.RemoveAvailabilitySlotAsync(listingId, date);

        // Assert
        count.Should().Be(1);

        using var assertContext = _fixture.CreateContext();
        
        var exists = await assertContext.AvailabilitySlots
            .AnyAsync(s => s.ListingId == listingId && s.Date == date);
    
        exists.Should().BeFalse("потому что мы удалили конкретно эту запись, а на остальные нам плевать");
    }
}