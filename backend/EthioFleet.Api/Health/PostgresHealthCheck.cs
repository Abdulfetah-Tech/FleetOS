using EthioFleet.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace EthioFleet.Api.Health;

public sealed class PostgresHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<PostgresHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or NpgsqlException)
        {
            logger.LogWarning(exception, "PostgreSQL readiness check failed");
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
        }
    }
}
