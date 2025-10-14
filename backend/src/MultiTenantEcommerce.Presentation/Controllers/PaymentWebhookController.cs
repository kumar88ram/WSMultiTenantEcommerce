using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/payment-webhooks")]
public class PaymentWebhookController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly IPaymentGatewayClient _paymentGatewayClient;

    public PaymentWebhookController(ICheckoutService checkoutService, IPaymentGatewayClient paymentGatewayClient)
    {
        _checkoutService = checkoutService;
        _paymentGatewayClient = paymentGatewayClient;
    }

    [HttpPost("stripe-like")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleStripeWebhook([FromBody] StripeLikeWebhookRequest request, CancellationToken cancellationToken)
    {
        var status = _paymentGatewayClient.TranslateStatus(request.Status);
        var payload = request.RawPayload is null ? null : JsonSerializer.Serialize(request.RawPayload);

        var webhook = new PaymentWebhookRequest(
            Provider: "stripe-like",
            EventType: request.Type,
            ProviderReference: request.PaymentIntentId,
            Status: status,
            Amount: request.AmountReceived,
            Currency: request.Currency,
            Payload: payload);

        var order = await _checkoutService.HandlePaymentWebhookAsync(webhook, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    public record StripeLikeWebhookRequest(
        string Type,
        string PaymentIntentId,
        string Status,
        decimal AmountReceived,
        string Currency,
        JsonElement? RawPayload);
}
