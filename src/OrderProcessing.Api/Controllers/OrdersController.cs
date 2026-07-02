using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Orders.Commands;
using OrderProcessing.Application.Orders.Queries;
using OrderProcessing.Contracts.Orders;
using OrderProcessing.Shared.Responses;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "CanReadOrders")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(request.CustomerId, request.ShippingAddress, request.Items);
        var result = await orders.CreateAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Fail("Order creation failed.", result.Errors.Select(x => x.Message), HttpContext.TraceIdentifier));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id },
            ApiResponse<OrderResponse>.Ok(result.Value, "Order created.", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [Authorize(Policy = "CanReadOrders")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] GetOrdersRequest request, CancellationToken cancellationToken)
    {
        var result = await orders.GetOrdersAsync(new GetOrdersQuery(request.Status, request.CustomerId, request.ToPagination()), cancellationToken);
        return Ok(ApiResponse<object>.Ok(result.Value, "Orders retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanReadOrders")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await orders.GetByIdAsync(new GetOrderByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ApiResponse<object>.Fail("Order not found.", result.Errors.Select(x => x.Message), HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<OrderResponse>.Ok(result.Value, "Order retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CanManageOrders")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await orders.UpdateStatusAsync(new UpdateStatusCommand(id, request.Status, request.Reason, User.Identity?.Name ?? "api"), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Fail("Status update failed.", result.Errors.Select(x => x.Message), HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<OrderResponse>.Ok(result.Value, "Order status updated.", HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanCancelOrders")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await orders.CancelAsync(new CancelOrderCommand(id, request.Reason, User.Identity?.Name ?? "api"), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Fail("Order cancellation failed.", result.Errors.Select(x => x.Message), HttpContext.TraceIdentifier));
        }

        return NoContent();
    }
}
