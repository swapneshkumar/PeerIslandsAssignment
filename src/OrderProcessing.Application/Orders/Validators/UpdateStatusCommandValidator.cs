using FluentValidation;
using OrderProcessing.Application.Orders.Commands;
using OrderProcessing.Domain.Orders;

namespace OrderProcessing.Application.Orders.Validators;

public sealed class UpdateStatusCommandValidator : AbstractValidator<UpdateStatusCommand>
{
    public UpdateStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Status).Must(s => s is OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered)
            .WithMessage("Status update must be Processing, Shipped, or Delivered.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ChangedBy).NotEmpty().MaximumLength(100);
    }
}
