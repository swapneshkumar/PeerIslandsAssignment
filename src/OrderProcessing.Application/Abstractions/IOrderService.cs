using OrderProcessing.Application.Orders.Commands;
using OrderProcessing.Application.Orders.Queries;
using OrderProcessing.Contracts.Orders;
using OrderProcessing.Shared.Pagination;
using OrderProcessing.Shared.Results;

namespace OrderProcessing.Application.Abstractions;

public interface IOrderService
{
    Task<Result<OrderResponse>> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> GetByIdAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<OrderResponse>>> GetOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> UpdateStatusAsync(UpdateStatusCommand command, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(CancelOrderCommand command, CancellationToken cancellationToken = default);
}
