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
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        Math.Clamp(configuration.GetValue<int?>("Notifications:ReminderIntervalMinutes") ?? 60, 5, 1440));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
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
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Fleet reminder cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
