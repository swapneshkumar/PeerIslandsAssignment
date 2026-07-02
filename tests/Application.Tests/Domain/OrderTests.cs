using FluentAssertions;
using OrderProcessing.Domain.Exceptions;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Domain.ValueObjects;

namespace Application.Tests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Create_order_without_items_is_rejected()
    {
        var order = NewOrder();

        var act = order.EnsureReadyForPlacement;

        act.Should().Throw<DomainException>()
            .WithMessage("Cannot create order without items.");
    }

    [Fact]
    public void Add_item_recalculates_total()
    {
        var order = NewOrder();

        order.AddItem(Guid.NewGuid(), "SKU-1", "Laptop", 2, Money.Create(100, "USD"));

        order.TotalAmount.Amount.Should().Be(200);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Processing_order_cannot_be_cancelled()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), "SKU-1", "Laptop", 1, Money.Create(100, "USD"));
        order.UpdateStatus(OrderStatus.Processing, "Start fulfillment.", "tester", DateTimeOffset.UtcNow);

        var act = () => order.Cancel("Customer requested.", "tester", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("Only pending orders can be cancelled.");
    }

    [Fact]
    public void Status_history_is_created_for_each_transition()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), "SKU-1", "Laptop", 1, Money.Create(100, "USD"));

        order.UpdateStatus(OrderStatus.Processing, "Start fulfillment.", "tester", DateTimeOffset.UtcNow);
        order.UpdateStatus(OrderStatus.Shipped, "Carrier accepted.", "tester", DateTimeOffset.UtcNow);

        order.StatusHistory.Should().HaveCount(3);
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    private static Order NewOrder()
        => Order.Create(
            Guid.NewGuid(),
            OrderNumber.Create("ORD-20260702-ABC"),
            Address.Create("1 Main", null, "Austin", "TX", "78701", "US"),
            DateTimeOffset.UtcNow);
}
