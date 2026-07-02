using OrderProcessing.Contracts.Orders;

namespace OrderProcessing.Application.Orders.Commands;

public sealed record CreateOrderCommand(Guid CustomerId, AddressDto ShippingAddress, IReadOnlyCollection<CreateOrderItemRequest> Items);
