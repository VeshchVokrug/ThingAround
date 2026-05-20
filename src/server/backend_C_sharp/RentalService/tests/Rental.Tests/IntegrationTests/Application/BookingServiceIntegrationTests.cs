using Core.SAGA.Contracts.Events;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using RentalService.Application.DTO;
using RentalService.Application.SAGA;
using RentalService.Application.Services;
using RentalService.Application.Services.Abstractions;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Repository;
using Xunit.Abstractions;

namespace Catalog.Tests.IntegrationTests.Application;

[Collection("PostgresCollection")]
public class BookingServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private FakeTimeProvider _fakeTime = null!;
    private readonly ITestOutputHelper _output;

    public BookingServiceIntegrationTests(PostgresFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _fakeTime = new FakeTimeProvider();
        _fakeTime.SetUtcNow(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var services = new ServiceCollection();

        services.AddDbContext<RentalDbContext>(options =>
        {
            options.UseNpgsql(_fixture.ConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingStatesRepository, BookingStatesRepository>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<TimeProvider>(_fakeTime);

        services.AddLogging(logging =>
        {
            logging.AddXUnit(_output);
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<BookingStateMachine, BookingState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<RentalDbContext>();
                });

            cfg.AddDelayedMessageScheduler();

            cfg.UsingInMemory((context, bus) =>
            {
                bus.UseDelayedMessageScheduler();
                bus.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider(true);

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        await ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnBookingId_WhenPendingApprovalReached()
    {
        var dto = CreateDto();

        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var createTask = bookingService.CreateAsync(dto, cts.Token);

        var bookingId = await WaitForBookingStateIdAsync(dto, cts.Token);
        await _harness.Bus.Publish(new CatalogSlotsReservedEvent(bookingId), cts.Token);

        var response = await createTask;

        response.BookingId.Should().Be(bookingId);
        await VerifyBookingStatusAsync(bookingId, BookingStatus.PendingApproval);
    }

    [Fact]
    public async Task ApproveBookingAsync_ShouldConfirmBooking()
    {
        var bookingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await SeedPendingApprovalAsync(bookingId, ownerId, tenantId);

        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await bookingService.ApproveBookingAsync(bookingId, ownerId, cts.Token);

        response.Success.Should().BeTrue();
        await VerifyBookingStatusAsync(bookingId, BookingStatus.Confirmed);
    }

    [Fact]
    public async Task RejectBookingAsync_ShouldRejectBooking()
    {
        var bookingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await SeedPendingApprovalAsync(bookingId, ownerId, tenantId);

        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await bookingService.RejectBookingAsync(bookingId, ownerId, "Price too low", cts.Token);

        response.Success.Should().BeTrue();
        await VerifyBookingStatusAsync(bookingId, BookingStatus.Rejected);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldCancelBooking()
    {
        var bookingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await SeedPendingApprovalAsync(bookingId, ownerId, tenantId);

        using var scope = _provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await bookingService.CancelBookingAsync(bookingId, tenantId, "Change of plans", cts.Token);

        response.Success.Should().BeTrue();
        await VerifyBookingStatusAsync(bookingId, BookingStatus.Cancelled);
    }

    private async Task SeedPendingApprovalAsync(Guid bookingId, Guid ownerId, Guid tenantId)
    {
        var request = new RentalBookingRequestedEvent(
            bookingId,
            Guid.NewGuid(),
            tenantId,
            ownerId,
            new DateOnly(2025, 10, 1),
            new DateOnly(2025, 10, 5),
            1000m);

        await _harness.Bus.Publish(request);

        await VerifyBookingStatusAsync(bookingId, BookingStatus.Created);
        await _harness.Bus.Publish(new CatalogSlotsReservedEvent(bookingId));
        await VerifyBookingStatusAsync(bookingId, BookingStatus.PendingApproval);
    }

    private CreateBookingDto CreateDto() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateOnly(2025, 10, 1),
        new DateOnly(2025, 10, 5),
        1000m);

    private async Task<Guid> WaitForBookingStateIdAsync(CreateBookingDto dto, CancellationToken ct)
    {
        Guid? bookingId = null;
        var success = await SpinWait(async () =>
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
            var state = await db.BookingStates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ListingId == dto.ListingId
                                          && x.TenantId == dto.TenantId
                                          && x.OwnerId == dto.OwnerId
                                          && x.StartDate == dto.StartDate
                                          && x.EndDate == dto.EndDate, ct);
            if (state == null)
            {
                return false;
            }
            bookingId = state.CorrelationId;
            return true;
        });

        success.Should().BeTrue("Booking state was not created");
        return bookingId!.Value;
    }

    private async Task VerifyBookingStatusAsync(Guid bookingId, BookingStatus expected)
    {
        var success = await SpinWait(async () =>
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookingId);
            return booking != null && booking.Status == expected;
        });

        success.Should().BeTrue($"Status {expected} not reached in DB for booking {bookingId}");
    }

    private static async Task<bool> SpinWait(Func<Task<bool>> condition)
    {
        var timeout = TimeSpan.FromSeconds(7);
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(150);
        }
        return false;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var db = _fixture.CreateContext();
        await db.BookingStates.ExecuteDeleteAsync();
        await db.Bookings.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }
}
