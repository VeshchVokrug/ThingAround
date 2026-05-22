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
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 1);
        await SeedListingAsync(context, listingId);

        // Act
        await sut.CreateRangeAsync(listingId, 500, new List<DateOnly> { date });
        await sut.SaveChangesAsync();

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
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        await SeedListingAsync(context, listingId);
    
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);
        var busyDate = today.AddDays(2);
    
        var busyDates = new List<DateOnly> { busyDate };

        // Act
        sut.PrepareInitialSlotsAsync(listingId, 100, busyDates);
        await sut.SaveChangesAsync();

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
    public async Task GetAvailabilitySlotsAsync_ShouldReturnAllSlotsWithin60Days()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);
        await SeedListingAsync(context, listingId);

        var validDate = today.AddDays(10);
        var tooFarDate = today.AddDays(61);

        context.AvailabilitySlots.AddRange(
            new AvailabilitySlot { ListingId = listingId, Date = validDate, Price = 100, IsAvailable = true },
            new AvailabilitySlot { ListingId = listingId, Date = today, Price = 100, IsAvailable = false },
            new AvailabilitySlot { ListingId = listingId, Date = tooFarDate, Price = 100, IsAvailable = true }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetTwoMonthSlotsAsync(listingId);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Date).Should().Contain([today, validDate]);
        result.Should().NotContain(x => x.Date == tooFarDate);
    }
    
    [Fact]
    public async Task TryReserveSlotsAsync_OwnerClosesDates_RepeatedCallShouldReturnFalseAndKeepReservation()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        Guid? bookingId = null;
        var date = new DateOnly(2026, 4, 15);
        await SeedListingAsync(context, listingId);
        
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
        await sut.SaveChangesAsync();
        
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
            secondResult.Should().BeFalse(); 

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
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var date = new DateOnly(2026, 4, 15);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await SeedListingAsync(context, listingId);

        context.AvailabilitySlots.Add(new AvailabilitySlot 
        { 
            ListingId = listingId, Date = date, IsAvailable = true, Price = 100 
        });
        await context.SaveChangesAsync();

        // Act
        var result = await sut.TryReserveSlotsAsync(listingId, [date], bookingId);
        await sut.SaveChangesAsync();

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
        await ResetDatabaseAsync(context);
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
        await sut.SaveChangesAsync();

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
        await ResetDatabaseAsync(context);
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
        await sut.SaveChangesAsync();

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
        await ResetDatabaseAsync(context);
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
        await sut.SaveChangesAsync();

        // Assert
        using var assertContext = _fixture.CreateContext();
        var slots = await assertContext.AvailabilitySlots
            .Where(s => s.ListingId == listingId)
            .OrderBy(s => s.Date).ToListAsync();
    
        slots[0].IsAvailable.Should().BeTrue();
        slots[1].IsAvailable.Should().BeFalse();
    }
    
    

    [Fact]
    public async Task RemoveAvailabilitySlotAsync_ShouldReturnDeletedCountAndRemoveFromDb()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 1);
        await SeedListingAsync(context, listingId);
    
        context.AvailabilitySlots.Add(new AvailabilitySlot { ListingId = listingId, Date = date, Price = 50 });
        await context.SaveChangesAsync();

        // Act
        var count = await sut.RemoveRangeAsync(listingId, new List<DateOnly> { date });
        await sut.SaveChangesAsync();

        // Assert
        count.Should().Be(1);

        using var assertContext = _fixture.CreateContext();
        
        var exists = await assertContext.AvailabilitySlots
            .AnyAsync(s => s.ListingId == listingId && s.Date == date);
    
        exists.Should().BeFalse("потому что мы удалили конкретно эту запись, а на остальные нам плевать");
    }

    private static async Task ResetDatabaseAsync(CatalogDbContext context)
    {
        await context.AvailabilitySlots.ExecuteDeleteAsync();
        await context.RentalListings.ExecuteDeleteAsync();
    }

    private static async Task SeedListingAsync(CatalogDbContext context, Guid listingId, bool isActive = true)
    {
        var listing = new RentalListing
        {
            Id = listingId,
            Version = 1,
            TitleSlug = $"listing-{listingId:N}",
            OwnerId = Guid.NewGuid(),
            CategorySlug = "Camping",
            Title = "Seed listing",
            Description = "Seed description",
            City = "Moscow",
            DefaultPrice = 100,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Contact = new ContactInfo
            {
                ManagerId = Guid.NewGuid(),
                PersonName = "Seed Manager",
                PersonPhone = "79990000000",
                SocialsUrls = null
            }
        };

        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();
    }
}