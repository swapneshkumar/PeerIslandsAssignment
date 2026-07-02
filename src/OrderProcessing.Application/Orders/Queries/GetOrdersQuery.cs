using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Pagination;

namespace OrderProcessing.Application.Orders.Queries;

public sealed record GetOrdersQuery(OrderStatus? Status, Guid? CustomerId, PaginationRequest Pagination);
