using EthioFleet.Application.Interfaces;

namespace EthioFleet.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
