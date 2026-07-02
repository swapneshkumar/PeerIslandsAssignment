using OrderProcessing.Domain.Orders;

namespace OrderProcessing.Application.Orders.Commands;

public sealed record UpdateStatusCommand(Guid OrderId, OrderStatus Status, string Reason, string ChangedBy);
