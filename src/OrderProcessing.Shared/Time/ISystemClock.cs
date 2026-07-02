namespace OrderProcessing.Shared.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
