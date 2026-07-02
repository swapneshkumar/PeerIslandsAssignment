using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Options;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Time;

namespace OrderProcessing.Infrastructure.Jobs;

public sealed class PendingOrderProcessorJob(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ISystemClock clock,
    IOptions<OrderProcessingOptions> options,
    ILogger<PendingOrderProcessorJob> logger) : IOrderProcessingJob
{
    public async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        var threshold = clock.UtcNow.AddMinutes(-options.Value.PendingOrderThresholdMinutes);
        var pendingOrders = await orders.GetPendingOlderThanAsync(threshold, options.Value.PendingOrderBatchSize, cancellationToken);

        foreach (var order in pendingOrders)
        {
            var existingHistoryIds = order.StatusHistory.Select(x => x.Id).ToHashSet();
            order.UpdateStatus(OrderStatus.Processing, "Automatically moved from pending threshold job.", "system", clock.UtcNow);
            var newHistory = order.StatusHistory.Single(x => !existingHistoryIds.Contains(x.Id));
            orders.TrackStatusChange(order, newHistory);
            logger.LogInformation("Order {OrderId} moved from Pending to Processing by background job.", order.Id);
        }

        if (pendingOrders.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
