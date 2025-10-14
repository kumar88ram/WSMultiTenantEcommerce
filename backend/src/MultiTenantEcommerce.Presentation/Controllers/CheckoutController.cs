using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;

    public CheckoutController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpGet("cart")]
    [AllowAnonymous]
    public async Task<ActionResult<CartDto>> GetCart([FromQuery] Guid? userId, [FromQuery] string? guestToken, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _checkoutService.GetOrCreateCartAsync(userId, guestToken, cancellationToken);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("cart/items")]
    [AllowAnonymous]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _checkoutService.AddItemToCartAsync(request, cancellationToken);
            return Ok(cart);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CheckoutResponse>> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _checkoutService.CheckoutAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
