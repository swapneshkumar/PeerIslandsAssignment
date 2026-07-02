using FluentValidation;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Mapping;
using OrderProcessing.Application.Options;
using OrderProcessing.Application.Orders.Commands;
using OrderProcessing.Application.Orders.Queries;
using OrderProcessing.Contracts.Orders;
using OrderProcessing.Domain.Exceptions;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Domain.ValueObjects;
using OrderProcessing.Shared.Pagination;
using OrderProcessing.Shared.Results;
using OrderProcessing.Shared.Time;

namespace OrderProcessing.Application.Orders.Services;

public sealed class OrderService(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ICacheService cache,
    ISystemClock clock,
    IValidator<CreateOrderCommand> createValidator,
    IValidator<UpdateStatusCommand> updateStatusValidator,
    IValidator<CancelOrderCommand> cancelValidator,
    IOptions<OrderProcessingOptions> options) : IOrderService
{
    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationFailure<OrderResponse>(validation.Errors.Select(x => x.ErrorMessage));
        }

        try
        {
            var address = Address.Create(
                command.ShippingAddress.Line1,
                command.ShippingAddress.Line2,
                command.ShippingAddress.City,
                command.ShippingAddress.State,
                command.ShippingAddress.PostalCode,
                command.ShippingAddress.Country);

            var order = Order.Create(command.CustomerId, OrderNumber.New(clock.UtcNow), address, clock.UtcNow);
            foreach (var item in command.Items)
            {
                order.AddItem(item.ProductId, item.ProductSku, item.ProductName, item.Quantity, Money.Create(item.UnitPrice, item.Currency));
            }

            order.EnsureReadyForPlacement();
            await orders.AddAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = order.ToResponse();
            await CacheOrderAsync(response, cancellationToken);
            return Result<OrderResponse>.Success(response);
        }
        catch (DomainException ex)
        {
            return Result<OrderResponse>.Failure(new Error("orders.business_rule", ex.Message));
        }
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(query.OrderId);
        var cached = await cache.GetAsync<OrderResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<OrderResponse>.Success(cached);
        }

        var order = await orders.GetDetailedByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<OrderResponse>.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var response = order.ToResponse();
        await CacheOrderAsync(response, cancellationToken);
        return Result<OrderResponse>.Success(response);
    }

    public async Task<Result<PagedResult<OrderResponse>>> GetOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var result = await orders.SearchAsync(query.Status, query.CustomerId, query.Pagination, cancellationToken);
        var page = new PagedResult<OrderResponse>(
            result.Items.Select(x => x.ToResponse()).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);

        return Result<PagedResult<OrderResponse>>.Success(page);
    }

    public async Task<Result<OrderResponse>> UpdateStatusAsync(UpdateStatusCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await updateStatusValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationFailure<OrderResponse>(validation.Errors.Select(x => x.ErrorMessage));
        }

        var order = await orders.GetDetailedByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<OrderResponse>.Failure(new Error("orders.not_found", "Order was not found."));
        }

        try
        {
            var existingHistoryIds = order.StatusHistory.Select(x => x.Id).ToHashSet();
            order.UpdateStatus(command.Status, command.Reason, command.ChangedBy, clock.UtcNow);
            var newHistory = order.StatusHistory.Single(x => !existingHistoryIds.Contains(x.Id));
            orders.TrackStatusChange(order, newHistory);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await cache.RemoveAsync(CacheKey(order.Id), cancellationToken);
            return Result<OrderResponse>.Success(order.ToResponse());
        }
        catch (DomainException ex)
        {
            return Result<OrderResponse>.Failure(new Error("orders.business_rule", ex.Message));
        }
    }

    public async Task<Result> CancelAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await cancelValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(x => new Error("validation", x.ErrorMessage)).ToArray());
        }

        var order = await orders.GetDetailedByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        try
        {
            var existingHistoryIds = order.StatusHistory.Select(x => x.Id).ToHashSet();
            order.Cancel(command.Reason, command.ChangedBy, clock.UtcNow);
            var newHistory = order.StatusHistory.Single(x => !existingHistoryIds.Contains(x.Id));
            orders.TrackStatusChange(order, newHistory);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await cache.RemoveAsync(CacheKey(order.Id), cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(new Error("orders.business_rule", ex.Message));
        }
    }

    private async Task CacheOrderAsync(OrderResponse response, CancellationToken cancellationToken)
        => await cache.SetAsync(CacheKey(response.Id), response, TimeSpan.FromMinutes(options.Value.OrderCacheMinutes), cancellationToken);

    private static string CacheKey(Guid orderId) => $"orders:{orderId:N}";

    private static Result<T> ValidationFailure<T>(IEnumerable<string> errors)
        => Result<T>.Failure(errors.Select(error => new Error("validation", error)).ToArray());
}
