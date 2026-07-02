namespace OrderProcessing.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
