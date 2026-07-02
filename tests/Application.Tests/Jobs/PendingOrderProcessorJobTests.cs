using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Options;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Domain.ValueObjects;
using OrderProcessing.Infrastructure.Jobs;
using OrderProcessing.Shared.Time;

namespace Application.Tests.Jobs;

public sealed class PendingOrderProcessorJobTests
{
    [Fact]
    public async Task ProcessPendingOrdersAsync_uses_five_minute_threshold_and_moves_pending_orders_to_processing()
    {
        var now = new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);
        var order = NewPendingOrder(now.AddMinutes(-6));
        var orders = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var clock = new Mock<ISystemClock>();
        var logger = new Mock<ILogger<PendingOrderProcessorJob>>();
        DateTimeOffset? requestedThreshold = null;

        clock.SetupGet(x => x.UtcNow).Returns(now);
        orders
            .Setup(x => x.GetPendingOlderThanAsync(It.IsAny<DateTimeOffset>(), 100, It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, int, CancellationToken>((threshold, _, _) => requestedThreshold = threshold)
            .ReturnsAsync([order]);
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var job = new PendingOrderProcessorJob(
            orders.Object,
            unitOfWork.Object,
            clock.Object,
            Options.Create(new OrderProcessingOptions()),
            logger.Object);

        await job.ProcessPendingOrdersAsync();

        requestedThreshold.Should().Be(now.AddMinutes(-5));
        order.Status.Should().Be(OrderStatus.Processing);
        order.StatusHistory.Should().ContainSingle(x => x.ToStatus == OrderStatus.Processing);
        orders.Verify(x => x.TrackStatusChange(order, It.Is<OrderStatusHistory>(history => history.ToStatus == OrderStatus.Processing)), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Order NewPendingOrder(DateTimeOffset createdAt)
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderNumber.Create("ORD-20260703-JOB"),
            Address.Create("1 Main", null, "Austin", "TX", "78701", "US"),
            createdAt);

        order.AddItem(Guid.NewGuid(), "SKU-1", "Laptop", 1, Money.Create(100, "USD"));
        return order;
    }
}
