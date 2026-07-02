using OrderProcessing.Domain.Exceptions;

namespace OrderProcessing.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a valid ISO 4217 code.");
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Multiply(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        return this with { Amount = Amount * quantity };
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Money currency mismatch.");
        }
    }
}
