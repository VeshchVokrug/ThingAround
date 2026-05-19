using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Repository;

namespace Catalog.Tests.IntegrationTests.Infrastructure;

[Collection("PostgresCollection")]
public class BookingRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public BookingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static BookingRepository CreateRepository(RentalDbContext context)
        => new(context);

    [Fact]
    public async Task GetAsync_ExistingBooking_ShouldReturnBooking()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var booking = CreateBooking(status: BookingStatus.Confirmed);
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        var sut = CreateRepository(context);

        var result = await sut.GetAsync(booking.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.TotalPrice.Should().Be(booking.TotalPrice);
        result.StartDate.Should().Be(booking.StartDate);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ShouldReturnNull()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var sut = CreateRepository(context);

        var result = await sut.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByOwnerAsync_ShouldReturnOnlyOwnerBookingsOrderedByCreatedAtDesc()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var older = CreateBooking(ownerId: ownerId, createdAt: new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero));
        var newer = CreateBooking(ownerId: ownerId, createdAt: new DateTimeOffset(2026, 5, 11, 10, 0, 0, TimeSpan.Zero));
        var foreign = CreateBooking(ownerId: otherOwnerId, createdAt: new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero));

        await context.Bookings.AddRangeAsync(older, newer, foreign);
        await context.SaveChangesAsync();

        var sut = CreateRepository(context);

        var result = (await sut.GetAllByOwnerAsync(ownerId)).ToList();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Fact]
    public async Task GetAllByTenantAsync_ShouldReturnOnlyTenantBookingsOrderedByCreatedAtDesc()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var older = CreateBooking(tenantId: tenantId, createdAt: new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));
        var newer = CreateBooking(tenantId: tenantId, createdAt: new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero));
        var foreign = CreateBooking(tenantId: otherTenantId, createdAt: new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero));

        await context.Bookings.AddRangeAsync(older, newer, foreign);
        await context.SaveChangesAsync();

        var sut = CreateRepository(context);

        var result = (await sut.GetAllByTenantAsync(tenantId)).ToList();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Fact]
    public async Task AddAsync_ValidBooking_ShouldPersistToDatabase()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var booking = CreateBooking(status: BookingStatus.Created, totalPrice: 4200m);
        var sut = CreateRepository(context);

        await sut.AddAsync(booking);
        await sut.SaveChangesAsync();

        await using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);

        saved.Should().NotBeNull();
        saved!.OwnerId.Should().Be(booking.OwnerId);
        saved.TenantId.Should().Be(booking.TenantId);
        saved.TotalPrice.Should().Be(4200m);
        saved.Status.Should().Be(BookingStatus.Created);
    }

    [Fact]
    public async Task UpdateAsync_WithMatchingVersion_ShouldUpdateFieldsAndIncrementVersion()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var booking = CreateBooking(status: BookingStatus.AwaitingConfirmation, version: 1, cancellationReason: null);
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        var updatedAt = new DateTimeOffset(2026, 5, 12, 9, 0, 0, TimeSpan.Zero);
        var expiresAt = updatedAt.AddHours(2);

        var dto = new UpdateBookingDto
        {
            Id = booking.Id,
            Version = 1,
            Status = BookingStatus.Confirmed,
            CancellationReason = "approved",
            UpdatedAt = updatedAt,
            ExpiresAt = expiresAt
        };

        var sut = CreateRepository(context);

        var updated = await sut.UpdateAsync(dto);
        await sut.SaveChangesAsync();

        updated.Should().BeTrue();

        await using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        saved.Status.Should().Be(BookingStatus.Confirmed);
        saved.CancellationReason.Should().Be("approved");
        saved.ExpiresAt.Should().Be(expiresAt);
        saved.UpdatedAt.Should().Be(updatedAt);
        saved.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WithWrongVersion_ShouldReturnFalseAndKeepOriginal()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);

        var booking = CreateBooking(status: BookingStatus.Created, version: 2, cancellationReason: "initial");
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        var dto = new UpdateBookingDto
        {
            Id = booking.Id,
            Version = 1,
            Status = BookingStatus.Cancelled,
            CancellationReason = "changed",
            UpdatedAt = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero)
        };

        var sut = CreateRepository(context);

        var updated = await sut.UpdateAsync(dto);
        await sut.SaveChangesAsync();

        updated.Should().BeFalse();

        await using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        saved.Status.Should().Be(BookingStatus.Created);
        saved.CancellationReason.Should().Be("initial");
        saved.Version.Should().Be(2);
    }

    private static async Task ResetDatabaseAsync(RentalDbContext context)
    {
        await context.Bookings.ExecuteDeleteAsync();
    }

    private static Booking CreateBooking(
        Guid? id = null,
        Guid? listingId = null,
        Guid? tenantId = null,
        Guid? ownerId = null,
        BookingStatus status = BookingStatus.Created,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal totalPrice = 1500m,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? expiresAt = null,
        uint version = 1,
        string? cancellationReason = null)
    {
        return new Booking
        {
            Id = id ?? Guid.NewGuid(),
            ListingId = listingId ?? Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            OwnerId = ownerId ?? Guid.NewGuid(),
            Status = status,
            StartDate = startDate ?? new DateOnly(2026, 5, 20),
            EndDate = endDate ?? new DateOnly(2026, 5, 22),
            TotalPrice = totalPrice,
            CreatedAt = createdAt ?? new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero),
            UpdatedAt = updatedAt ?? new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero),
            ExpiresAt = expiresAt,
            Version = version,
            CancellationReason = cancellationReason
        };
    }
}