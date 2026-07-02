namespace OrderProcessing.Application.Abstractions;

public interface IOrderProcessingJob
{
    Task ProcessPendingOrdersAsync(CancellationToken cancellationToken = default);
}
