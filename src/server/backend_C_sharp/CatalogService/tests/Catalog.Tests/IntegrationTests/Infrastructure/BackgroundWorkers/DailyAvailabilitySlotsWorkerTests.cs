using Application.Services;
using Application.Services.Abstractions;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.BackgroundWorkers;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CatalogService.Tests.IntegrationTests.Infrastructure.BackgroundWorkers;

[Collection("PostgresCollection")]
public class DailyAvailabilitySlotsWorkerTests
{
    private readonly PostgresFixture _fixture;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DailyAvailabilitySlotsWorker> _logger;
    private readonly ServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts;

    public DailyAvailabilitySlotsWorkerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));
        _cts = new CancellationTokenSource();
        _logger = Substitute.For<ILogger<DailyAvailabilitySlotsWorker>>();

        var services = new ServiceCollection();

        services.AddScoped(_ => _fixture.CreateContext());
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<IListingQueryRepository, ListingQueryRepository>();
        services.AddScoped<IUpdateSlotsUseCase, UpdateSlotsUseCase>();
        services.AddSingleton<TimeProvider>(_timeProvider);
        services.AddSingleton<ILogger<DailyAvailabilitySlotsWorker>>(_logger);
        
        services.AddHostedService<DailyAvailabilitySlotsWorker>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Worker_WhenTriggered_ShouldPerformDailyCleanupAndCreationInPostgres()
    {
        // Arrange
        using var initContext = _fixture.CreateContext();
        await ResetDatabaseAsync(initContext);

        var listingId = Guid.NewGuid();
        await SeedListingAsync(initContext, listingId, 150);

        // Устаревший слот
        var expiredDate = new DateOnly(2026, 4, 9);
        await SeedSlotAsync(initContext, listingId, expiredDate, 150);

        var worker = _serviceProvider.GetServices<IHostedService>()
            .OfType<DailyAvailabilitySlotsWorker>()
            .Single();

        // Запуск службы
        await worker.StartAsync(_cts.Token);

        // Ждем 100 мс реального времени, чтобы фоновый поток воркера 
        // гарантированно дошел до точки ожидания таймера 'await Task.Delay'
        await Task.Delay(100);

        // Act
        // Теперь, когда воркер гарантированно спит, переводим виртуальное время на 1 день вперед
        _timeProvider.Advance(TimeSpan.FromDays(1));

        // Опрашиваем базу данных
        List<AvailabilitySlot> slots = [];
        bool isCompletedSuccessfully = false;

        for (int i = 0; i < 30; i++)
        {
            // Проверка логов на исключения
            var errorCall = _logger.ReceivedCalls()
                .FirstOrDefault(c => c.GetArguments().Length > 0 && 
                                     c.GetArguments()[0] is LogLevel level && 
                                     level == LogLevel.Error);

            if (errorCall != null)
            {
                var exception = errorCall.GetArguments().OfType<Exception>().FirstOrDefault();
                var message = errorCall.GetArguments()[2]?.ToString();
                throw new Xunit.Sdk.XunitException($"Worker execution failed with error: '{message}'. Exception: {exception}");
            }

            using var checkContext = _fixture.CreateContext();
            slots = await checkContext.AvailabilitySlots
                .Where(s => s.ListingId == listingId)
                .ToListAsync();

            if (!slots.Any(s => s.Date == expiredDate) && slots.Count > 0)
            {
                isCompletedSuccessfully = true;
                break;
            }

            await Task.Delay(100);
        }

        if (!isCompletedSuccessfully)
        {
            using var diagContext = _fixture.CreateContext();
            var totalListings = await diagContext.RentalListings.CountAsync();
            var totalSlots = await diagContext.AvailabilitySlots.ToListAsync();

            var logEntries = _logger.ReceivedCalls()
                .Select(call =>
                {
                    var args = call.GetArguments();
                    var level = args.ElementAtOrDefault(0)?.ToString() ?? "Unknown";
                    var message = args.ElementAtOrDefault(2)?.ToString();
                    
                    if (args.Length >= 5 && args[4] is Delegate formatter)
                    {
                        try
                        {
                            message = formatter.DynamicInvoke(args[2], args[3])?.ToString();
                        }
                        catch { }
                    }
                    return $"[{level}] {message}";
                })
                .ToList();

            var logSummary = string.Join(Environment.NewLine, logEntries);
            var dbState = $"Listings in DB: {totalListings}. Slots in DB: {totalSlots.Count} (Dates: {string.Join(", ", totalSlots.Select(s => s.Date))})";
            var timeState = $"Virtual Time on Failure: {_timeProvider.GetUtcNow()}";

            throw new Xunit.Sdk.XunitException(
                $"Background service did not process slots within timeout.{Environment.NewLine}" +
                $"--- DATABASE STATE ---{Environment.NewLine}{dbState}{Environment.NewLine}" +
                $"--- TIME STATE ---{Environment.NewLine}{timeState}{Environment.NewLine}" +
                $"--- WORKER LOGS ---{Environment.NewLine}{logSummary}");
        }

        // Assert
        slots.Should().NotContain(s => s.Date == expiredDate);
        slots.Should().Contain(s => s.Date == new DateOnly(2026, 4, 11));
    }

    private static async Task ResetDatabaseAsync(CatalogDbContext context)
    {
        await context.AvailabilitySlots.ExecuteDeleteAsync();
        await context.RentalListings.ExecuteDeleteAsync();
    }

    private async Task SeedListingAsync(CatalogDbContext context, Guid listingId, int defaultPrice)
    {
     
        var listing = new RentalListing
        {
            Id = listingId,
            Version = 1,
            OwnerId = Guid.NewGuid(),
            CategorySlug = "test-category",
            Title = "Тестовое объявление",
            TitleSlug = "testovoe-obyavlenie",
            Description = "Описание тестового объявления",
            City = "Москва",
            OwnerRating = 5.0f,
            DefaultPrice = defaultPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            
            Contact = new ContactInfo 
            {
                ManagerId = Guid.NewGuid(),
                PersonName = "Test",
                PersonPhone = "1234567890"
            }
        };

        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();
    }

    private async Task SeedSlotAsync(CatalogDbContext context, Guid listingId, DateOnly date, int price)
    {
        var slot = new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            Price = price,
            Version = 1,
            IsAvailable = true
        };
        await context.AvailabilitySlots.AddAsync(slot);
        await context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        await _serviceProvider.DisposeAsync();
    }
}