using OrderProcessing.Domain.Common;

namespace OrderProcessing.Domain.Orders;

public sealed class OrderStatusHistory : Entity
{
    private OrderStatusHistory() { }

    internal OrderStatusHistory(Guid orderId, OrderStatus fromStatus, OrderStatus toStatus, string reason, string changedBy, DateTimeOffset changedAt)
    {
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }
}
