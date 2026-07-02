using FluentAssertions;
using OrderProcessing.Application.Orders.Commands;
using OrderProcessing.Application.Orders.Validators;
using OrderProcessing.Contracts.Orders;

namespace Application.Tests.Validators;

public sealed class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public async Task Rejects_empty_order_items()
    {
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new AddressDto("1 Main", null, "Austin", "TX", "78701", "US"),
            []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Items");
    }

    [Fact]
    public async Task Rejects_zero_quantity()
    {
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new AddressDto("1 Main", null, "Austin", "TX", "78701", "US"),
            [new CreateOrderItemRequest(Guid.NewGuid(), "SKU-1", "Laptop", 0, 50, "USD")]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.EndsWith("Quantity"));
    }
}
