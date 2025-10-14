using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Presentation.Controllers.Storefront;

[ApiController]
[Route("store/{tenant}/checkout")]
[AllowAnonymous]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly ITenantResolver _tenantResolver;

    public CheckoutController(ICheckoutService checkoutService, ITenantResolver tenantResolver)
    {
        _checkoutService = checkoutService;
        _tenantResolver = tenantResolver;
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(CheckoutConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions(string tenant, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var configuration = await _checkoutService.GetCheckoutConfigurationAsync(cancellationToken);
        return Ok(configuration);
    }

    [HttpPost("session")]
    [ProducesResponseType(typeof(CheckoutSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSession(string tenant, [FromBody] CreateCheckoutSessionRequestDto request, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var session = await _checkoutService.CreateCheckoutSessionAsync(request, cancellationToken);
        return Ok(session);
    }

    [HttpGet("status/{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(string tenant, Guid orderId, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var status = await _checkoutService.GetPaymentStatusAsync(orderId, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
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
