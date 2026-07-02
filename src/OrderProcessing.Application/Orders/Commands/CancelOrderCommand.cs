namespace OrderProcessing.Application.Orders.Commands;

public sealed record CancelOrderCommand(Guid OrderId, string Reason, string ChangedBy);
