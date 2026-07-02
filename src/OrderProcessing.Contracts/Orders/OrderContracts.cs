using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Pagination;

namespace OrderProcessing.Contracts.Orders;

public sealed record AddressDto(string Line1, string? Line2, string City, string State, string PostalCode, string Country);

public sealed record CreateOrderItemRequest(Guid ProductId, string ProductSku, string ProductName, int Quantity, decimal UnitPrice, string Currency);

public sealed record CreateOrderRequest(Guid CustomerId, AddressDto ShippingAddress, IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record UpdateOrderStatusRequest(OrderStatus Status, string Reason);

public sealed record CancelOrderRequest(string Reason);

public sealed record OrderItemResponse(Guid Id, Guid ProductId, string ProductSku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, string Currency);

public sealed record OrderStatusHistoryResponse(OrderStatus FromStatus, OrderStatus ToStatus, string Reason, string ChangedBy, DateTimeOffset ChangedAt);

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    AddressDto ShippingAddress,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<OrderItemResponse> Items,
    IReadOnlyCollection<OrderStatusHistoryResponse> StatusHistory);

public sealed record GetOrdersRequest(
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    string? SortBy = null,
    string? SortDirection = "desc")
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize, SortBy, SortDirection);
}
