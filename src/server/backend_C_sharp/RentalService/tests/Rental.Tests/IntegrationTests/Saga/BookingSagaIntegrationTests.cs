using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RentalService.Application.SAGA;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Repository;
using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace Catalog.Tests.IntegrationTests.Saga;

[Collection("PostgresCollection")]
public class BookingSagaIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingState> _sagaHarness = null!;
    private FakeTimeProvider _fakeTime = null!; 
    private readonly ITestOutputHelper _output;

    public BookingSagaIntegrationTests(PostgresFixture fixture, ITestOutputHelper output)
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
        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _harness.GetSagaStateMachineHarness<BookingStateMachine, BookingState>();

        await _harness.Start();
        await ResetDatabaseAsync();
    }

    [Fact]
    public async Task Saga_HappyPath_ShouldConfirmBooking()
    {
        var bookingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        await _harness.Bus.Publish(CreateRequest(bookingId));
        
        (await _sagaHarness.Consumed.Any<RentalBookingRequestedEvent>(x => x.Context.Message.BookingId == bookingId)).Should().BeTrue();
        await VerifyDbStatus(bookingId, BookingStatus.Created);
        
        await _harness.Bus.Publish(new CatalogSlotsReservedEvent(bookingId));
        (await _sagaHarness.Consumed.Any<CatalogSlotsReservedEvent>()).Should().BeTrue();
        await VerifyDbStatus(bookingId, BookingStatus.PendingApproval);
        
        await _harness.Bus.Publish(new RentalBookingApprovedEvent(bookingId, ownerId));
        
        (await _sagaHarness.Consumed.Any<RentalBookingApprovedEvent>()).Should().BeTrue();
        await VerifyDbStatus(bookingId, BookingStatus.Confirmed);
        
        var sagaExists = await _sagaHarness.Exists(bookingId, x => x.Final);
        sagaExists.Should().BeNull("Saga should be removed from DB after completion");
    }

    [Fact]
    public async Task Saga_CatalogFails_ShouldRejectBooking()
    {
        var bookingId = Guid.NewGuid();
        await _harness.Bus.Publish(CreateRequest(bookingId));
        await _sagaHarness.Created.Any(x => x.CorrelationId == bookingId);

        await _harness.Bus.Publish(new CatalogSlotsReservationFailedEvent(bookingId, "No capacity"));

        (await _sagaHarness.Consumed.Any<CatalogSlotsReservationFailedEvent>()).Should().BeTrue();
        await VerifyDbStatus(bookingId, BookingStatus.Rejected);
    }

    [Fact]
    public async Task Saga_OwnerRejected_ShouldReleaseSlots()
    {
        var bookingId = Guid.NewGuid();
        
        await _harness.Bus.Publish(CreateRequest(bookingId));
        
        (await _sagaHarness.Created.Any(x => x.CorrelationId == bookingId)).Should().BeTrue();
        await WaitForSagaState(bookingId, "AwaitingCatalogReservation"); // <--- Важно!
        
        await _harness.Bus.Publish(new CatalogSlotsReservedEvent(bookingId));
        
        await WaitForSagaState(bookingId, "AwaitingOwnerApproval");
        
        await _harness.Bus.Publish(new RentalBookingRejectedEvent(bookingId, Guid.NewGuid(), "Price too low"));
        
        (await _harness.Published.Any<CatalogReleaseSlots>(x => x.Context!.Message.BookingId == bookingId)).Should().BeTrue();
        await VerifyDbStatus(bookingId, BookingStatus.Rejected);
    }

    [Fact]
    public async Task Saga_Timeout_ShouldAutomaticallyExpire()
    {
        var bookingId = Guid.NewGuid();
        
        await _harness.Bus.Publish(CreateRequest(bookingId));
        await WaitForSagaState(bookingId, "AwaitingCatalogReservation");

        await _harness.Bus.Publish(new CatalogSlotsReservedEvent(bookingId));
        await WaitForSagaState(bookingId, "AwaitingOwnerApproval");

        await Task.Delay(500);
        
        await _harness.Bus.Publish(new RentalBookingExpiredEvent(bookingId));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var published = await _harness.Published.Any<CatalogReleaseSlots>(
            x => x.Context!.Message.BookingId == bookingId, 
            cts.Token 
        );

        published.Should().BeTrue("CatalogReleaseSlots was not published");
        await VerifyDbStatus(bookingId, BookingStatus.Expired);
    }

    #region Helpers

    private RentalBookingRequestedEvent CreateRequest(Guid id) => 
        new(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 
            new DateOnly(2025, 10, 1), new DateOnly(2025, 10, 5), 1000m);

    private async Task VerifyDbStatus(Guid bookingId, BookingStatus expected)
    {
        var success = await SpinWait(async () =>
        {
            await using var db = _fixture.CreateContext();
            var b = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookingId);
            return b != null && b.Status == expected;
        });
        success.Should().BeTrue($"Status {expected} not reached in DB for booking {bookingId}");
    }

    private async Task WaitForSagaState(Guid bookingId, string stateName)
    {
        var success = await SpinWait(async () =>
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
            var s = await db.Set<BookingState>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.CorrelationId == bookingId);
            return s != null && s.CurrentState == stateName;
        });
        success.Should().BeTrue($"Saga {bookingId} did not reach state {stateName}");
    }

    private async Task<bool> SpinWait(Func<Task<bool>> condition)
    {
        var timeout = TimeSpan.FromSeconds(7);
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
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
    #endregion
}