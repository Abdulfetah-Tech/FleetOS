using EthioFleet.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EthioFleet.Infrastructure.Services;

public sealed class FleetReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FleetReminderBackgroundService> logger) : BackgroundService
{
    private readonly bool _enabled =
        configuration.GetValue<bool?>("Notifications:ReminderEnabled") ?? true;

    // Gives the database and migrations time to be ready before the first cycle.
    private readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(
        Math.Clamp(configuration.GetValue<int?>("Notifications:ReminderStartupDelaySeconds") ?? 30, 0, 600));

    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        Math.Clamp(configuration.GetValue<int?>("Notifications:ReminderIntervalMinutes") ?? 60, 5, 1440));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            logger.LogInformation("Fleet reminder service is disabled by configuration");
            return;
        }

        try
        {
            if (_startupDelay > TimeSpan.Zero)
            {
                await Task.Delay(_startupDelay, stoppingToken);
            }

            using var timer = new PeriodicTimer(_interval);
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IFleetReminderProcessor>();
            var created = await processor.GenerateDueRemindersAsync(
                DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
            logger.LogInformation("Fleet reminder cycle completed; {NotificationCount} notifications created", created);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // One failed cycle must not stop future cycles.
            logger.LogError(exception, "Fleet reminder cycle failed");
        }
    }
}