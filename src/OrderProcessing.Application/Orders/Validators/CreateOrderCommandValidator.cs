using FluentValidation;
using OrderProcessing.Application.Orders.Commands;

namespace OrderProcessing.Application.Orders.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ShippingAddress).NotNull();
        RuleFor(x => x.ShippingAddress.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShippingAddress.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShippingAddress.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ShippingAddress.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.ProductSku).NotEmpty().MaximumLength(64);
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.Currency).NotEmpty().Length(3);
        });
    }
}
