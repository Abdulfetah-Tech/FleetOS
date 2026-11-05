namespace EthioFleet.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
