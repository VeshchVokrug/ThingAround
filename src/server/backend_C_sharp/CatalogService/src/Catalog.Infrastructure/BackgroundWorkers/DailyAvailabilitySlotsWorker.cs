using Application.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundWorkers;

public class DailyAvailabilitySlotsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DailyAvailabilitySlotsWorker> _logger;

    private readonly TimeSpan _targetTime = TimeSpan.FromHours(3); 
    private readonly TimeSpan _targetOffset = TimeSpan.FromHours(5); 
    
    public DailyAvailabilitySlotsWorker(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<DailyAvailabilitySlotsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Availability Worker started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            _logger.LogInformation("The next launch is scheduled in {Delay}", delay);

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            await TryExecuteJobAsync(stoppingToken);
        }
    }
    
    private async Task TryExecuteJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start execution Daily Availability Worker.");

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var useCase = scope.ServiceProvider.GetRequiredService<IUpdateSlotsUseCase>();
                
                await useCase.RemoveExpiredAndCreateNewSlotsAsync(stoppingToken);
            }

            _logger.LogInformation("End execution Daily Availability Worker.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error occurred during execution.");
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var nowInTargetZone = nowUtc.ToOffset(_targetOffset);
        
        var nextRunInTargetZone = nowInTargetZone.Date.Add(_targetTime);

        if (nowInTargetZone.DateTime >= nextRunInTargetZone)
        {
            nextRunInTargetZone = nextRunInTargetZone.AddDays(1);
        }

        var nextRunOffset = new DateTimeOffset(nextRunInTargetZone, _targetOffset);
        return nextRunOffset - nowUtc;
    }
}