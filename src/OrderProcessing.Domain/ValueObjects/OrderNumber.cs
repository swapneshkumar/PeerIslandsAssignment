using OrderProcessing.Domain.Exceptions;

namespace OrderProcessing.Domain.ValueObjects;

public sealed record OrderNumber(string Value)
{
    public static OrderNumber New(DateTimeOffset now)
        => new($"ORD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant());

    public static OrderNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            throw new DomainException("Order number is invalid.");
        }

        return new OrderNumber(value.Trim().ToUpperInvariant());
    }
}
