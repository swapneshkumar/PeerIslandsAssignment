using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Pagination;

namespace OrderProcessing.Persistence.Repositories;

public sealed class OrderRepository(OrderProcessingDbContext dbContext) : Repository<Order>(dbContext), IOrderRepository
{
    private IQueryable<Order> DetailedOrders => DbContext.Orders
        .Include(x => x.Items)
        .Include(x => x.StatusHistory)
        .AsSplitQuery();

    public async Task<Order?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DetailedOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<Order>> SearchAsync(OrderStatus? status, Guid? customerId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var query = DetailedOrders.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (customerId is not null)
        {
            query = query.Where(x => x.CustomerId == customerId);
        }

        query = pagination.SortBy?.ToLowerInvariant() switch
        {
            "total" => pagination.SortDirection == "asc" ? query.OrderBy(x => x.TotalAmount.Amount) : query.OrderByDescending(x => x.TotalAmount.Amount),
            "status" => pagination.SortDirection == "asc" ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status),
            _ => pagination.SortDirection == "asc" ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(pagination.Skip).Take(pagination.Take).ToArrayAsync(cancellationToken);

        return new PagedResult<Order>(items, Math.Max(pagination.PageNumber, 1), pagination.Take, total);
    }

    public async Task<IReadOnlyCollection<Order>> GetPendingOlderThanAsync(DateTimeOffset threshold, int take, CancellationToken cancellationToken = default)
        => await DetailedOrders
            .Where(x => x.Status == OrderStatus.Pending && x.CreatedAt <= threshold)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToArrayAsync(cancellationToken);

    public void TrackStatusChange(Order order, OrderStatusHistory statusHistory)
    {
        var status = order.Status;
        var updatedAt = order.UpdatedAt;

        foreach (var history in order.StatusHistory.Where(x => x.Id != statusHistory.Id))
        {
            DbContext.Entry(history).State = EntityState.Unchanged;
        }

        DbContext.Entry(order).State = EntityState.Unchanged;
        DbContext.Entry(order).Property(x => x.Status).CurrentValue = status;
        DbContext.Entry(order).Property(x => x.UpdatedAt).CurrentValue = updatedAt;
        DbContext.Entry(order).Property(x => x.Status).IsModified = true;
        DbContext.Entry(order).Property(x => x.UpdatedAt).IsModified = true;
        DbContext.Entry(statusHistory).State = EntityState.Added;
    }
}
