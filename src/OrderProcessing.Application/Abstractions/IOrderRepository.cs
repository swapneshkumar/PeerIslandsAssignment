using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Pagination;

namespace OrderProcessing.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> SearchAsync(OrderStatus? status, Guid? customerId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetPendingOlderThanAsync(DateTimeOffset threshold, int take, CancellationToken cancellationToken = default);
    void TrackStatusChange(Order order, OrderStatusHistory statusHistory);
}
