using OrderProcessing.Domain.Common;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Domain.Customers;

public sealed class Customer : Entity
{
    private Customer() { }

    private Customer(string fullName, string email, Address shippingAddress)
    {
        FullName = fullName;
        Email = email;
        ShippingAddress = shippingAddress;
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address ShippingAddress { get; private set; } = null!;

    public static Customer Create(string fullName, string email, Address shippingAddress)
        => new(fullName.Trim(), email.Trim().ToLowerInvariant(), shippingAddress);
}
