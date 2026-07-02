using OrderProcessing.Domain.Common;
using OrderProcessing.Domain.Exceptions;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem() { }

    internal OrderItem(Guid productId, string productSku, string productName, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = unitPrice.Multiply(quantity);
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money LineTotal { get; private set; } = null!;
}
