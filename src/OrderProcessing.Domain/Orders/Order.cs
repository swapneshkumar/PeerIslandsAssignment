using OrderProcessing.Domain.Common;
using OrderProcessing.Domain.Exceptions;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order() { }

    private Order(Guid customerId, OrderNumber orderNumber, Address shippingAddress, DateTimeOffset createdAt)
    {
        CustomerId = customerId;
        OrderNumber = orderNumber;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        TotalAmount = Money.Zero();
        _statusHistory.Add(new OrderStatusHistory(Id, OrderStatus.Pending, OrderStatus.Pending, "Order created.", "system", createdAt));
    }

    public Guid CustomerId { get; private set; }
    public OrderNumber OrderNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public Address ShippingAddress { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static Order Create(Guid customerId, OrderNumber orderNumber, Address shippingAddress, DateTimeOffset createdAt)
        => new(customerId, orderNumber, shippingAddress, createdAt);

    public void AddItem(Guid productId, string sku, string name, int quantity, Money unitPrice)
    {
        _items.Add(new OrderItem(productId, sku, name, quantity, unitPrice));
        RecalculateTotal();
    }

    public void EnsureReadyForPlacement()
    {
        if (_items.Count == 0)
        {
            throw new DomainException("Cannot create order without items.");
        }
    }

    public void UpdateStatus(OrderStatus nextStatus, string reason, string changedBy, DateTimeOffset changedAt)
    {
        if (Status == nextStatus)
        {
            return;
        }

        if (!IsTransitionAllowed(Status, nextStatus))
        {
            throw new DomainException($"Cannot transition order from {Status} to {nextStatus}.");
        }

        var previous = Status;
        Status = nextStatus;
        UpdatedAt = changedAt;
        _statusHistory.Add(new OrderStatusHistory(Id, previous, nextStatus, reason.Trim(), changedBy.Trim(), changedAt));
    }

    public void Cancel(string reason, string changedBy, DateTimeOffset changedAt)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can be cancelled.");
        }

        UpdateStatus(OrderStatus.Cancelled, reason, changedBy, changedAt);
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Aggregate(Money.Zero(_items[0].UnitPrice.Currency), (total, item) => total.Add(item.LineTotal));
    }

    private static bool IsTransitionAllowed(OrderStatus current, OrderStatus next) =>
        (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
}
