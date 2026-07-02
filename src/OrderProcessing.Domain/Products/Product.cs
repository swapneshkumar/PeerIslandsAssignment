using OrderProcessing.Domain.Common;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Domain.Products;

public sealed class Product : Entity
{
    private Product() { }

    private Product(string sku, string name, Money price)
    {
        Sku = sku;
        Name = name;
        Price = price;
        IsActive = true;
    }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public static Product Create(string sku, string name, Money price) => new(sku.Trim().ToUpperInvariant(), name.Trim(), price);
}
