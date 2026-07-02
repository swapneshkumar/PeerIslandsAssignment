using OrderProcessing.Domain.Exceptions;

namespace OrderProcessing.Domain.ValueObjects;

public sealed record Address(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    public static Address Create(string line1, string? line2, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(state) ||
            string.IsNullOrWhiteSpace(postalCode) ||
            string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException("Address is incomplete.");
        }

        return new Address(line1.Trim(), line2?.Trim(), city.Trim(), state.Trim(), postalCode.Trim(), country.Trim());
    }
}
