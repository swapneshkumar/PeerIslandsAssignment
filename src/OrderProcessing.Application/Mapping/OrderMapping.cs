using OrderProcessing.Contracts.Orders;
using OrderProcessing.Domain.Orders;

namespace OrderProcessing.Application.Mapping;

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.OrderNumber.Value,
            order.CustomerId,
            order.Status,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            new AddressDto(
                order.ShippingAddress.Line1,
                order.ShippingAddress.Line2,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country),
            order.CreatedAt,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.ProductId,
                item.ProductSku,
                item.ProductName,
                item.Quantity,
                item.UnitPrice.Amount,
                item.LineTotal.Amount,
                item.UnitPrice.Currency)).ToArray(),
            order.StatusHistory.OrderBy(x => x.ChangedAt).Select(history => new OrderStatusHistoryResponse(
                history.FromStatus,
                history.ToStatus,
                history.Reason,
                history.ChangedBy,
                history.ChangedAt)).ToArray());
}
