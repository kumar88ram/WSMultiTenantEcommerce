using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Presentation.Controllers.Storefront;

[ApiController]
[Route("store/{tenant}/refunds")]
[Authorize]
public class RefundRequestsController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ITenantResolver _tenantResolver;

    public RefundRequestsController(IRefundService refundService, ITenantResolver tenantResolver)
    {
        _refundService = refundService;
        _tenantResolver = tenantResolver;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRefund(string tenant, [FromBody] CreateRefundRequestDto request, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var refund = await _refundService.SubmitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRefund), new { tenant, refundId = refund.Id }, refund);
    }

    [HttpGet("{refundId:guid}")]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRefund(string tenant, Guid refundId, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var refund = await _refundService.GetByIdAsync(refundId, cancellationToken);
        return refund is null ? NotFound() : Ok(refund);
    }

    private void EnsureTenantContext(string tenant)
    {
        if (!string.IsNullOrWhiteSpace(_tenantResolver.TenantIdentifier) &&
            !string.Equals(_tenantResolver.TenantIdentifier, tenant, StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["X-Tenant-Mismatch"] = _tenantResolver.TenantIdentifier;
        }
    }
}
