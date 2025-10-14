using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("api/store-admin/refunds")]
[Authorize]
public class RefundRequestsController : ControllerBase
{
    private readonly IRefundService _refundService;

    public RefundRequestsController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RefundRequestListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefunds([FromQuery] RefundQueryParameters query, CancellationToken cancellationToken)
    {
        var result = await _refundService.GetAsync(new RefundRequestListQuery(query.Page, query.PageSize, query.Status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{refundId:guid}")]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRefund(Guid refundId, CancellationToken cancellationToken)
    {
        var refund = await _refundService.GetByIdAsync(refundId, cancellationToken);
        return refund is null ? NotFound() : Ok(refund);
    }

    [HttpPost("{refundId:guid}/approve")]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid refundId, [FromBody] RefundDecisionRequest request, CancellationToken cancellationToken)
    {
        var refund = await _refundService.ApproveAsync(refundId, request, cancellationToken);
        return Ok(refund);
    }

    [HttpPost("{refundId:guid}/deny")]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deny(Guid refundId, [FromBody] RefundDecisionRequest request, CancellationToken cancellationToken)
    {
        var refund = await _refundService.DenyAsync(refundId, request, cancellationToken);
        return Ok(refund);
    }

    public class RefundQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public RefundRequestStatus? Status { get; set; }
    }
}
