using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("api/store-admin/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly IRefundService _refundService;

    public OrdersController(ICheckoutService checkoutService, IRefundService refundService)
    {
        _checkoutService = checkoutService;
        _refundService = refundService;
    }

    [HttpGet]
    public async Task<ActionResult<OrderListResult>> GetOrders([FromQuery] OrderQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = new OrderListQuery(
            parameters.Page,
            parameters.PageSize,
            parameters.Status,
            parameters.From,
            parameters.To,
            parameters.Search);

        var orders = await _checkoutService.GetOrdersAsync(query, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _checkoutService.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPut("{orderId:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid orderId, [FromBody] UpdateOrderStatusBody request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _checkoutService.UpdateOrderStatusAsync(orderId, request.Status, request.TrackingNumber, cancellationToken);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{orderId:guid}/refund")]
    public async Task<ActionResult<OrderDto>> Refund(Guid orderId, [FromBody] OrderRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _refundService.CreateImmediateRefundAsync(orderId, request, cancellationToken);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public class OrderQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public OrderStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

    public class UpdateOrderStatusBody
    {
        public OrderStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
    }
}
