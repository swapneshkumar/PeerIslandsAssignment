using FluentValidation;
using OrderProcessing.Application.Orders.Commands;

namespace OrderProcessing.Application.Orders.Validators;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ChangedBy).NotEmpty().MaximumLength(100);
    }
}
