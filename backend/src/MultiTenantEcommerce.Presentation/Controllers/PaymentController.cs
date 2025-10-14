using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly IPaymentGatewayOrchestrator _paymentGatewayOrchestrator;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ICheckoutService checkoutService,
        IPaymentGatewayOrchestrator paymentGatewayOrchestrator,
        ILogger<PaymentController> logger)
    {
        _checkoutService = checkoutService;
        _paymentGatewayOrchestrator = paymentGatewayOrchestrator;
        _logger = logger;
    }

    [HttpPost("initiate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CheckoutResponse>> InitiatePayment([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _checkoutService.CheckoutAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Checkout failed due to invalid state");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Checkout failed due to bad argument");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{provider}/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveWebhook(string provider, CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.ToLowerInvariant();
        var payload = await ReadPayloadAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest("Webhook payload cannot be empty.");
        }

        var signature = ResolveSignature(normalizedProvider);
        var eventType = ResolveEventType();
        var headers = ToDictionary(Request.Headers);
        var verificationRequest = new PaymentVerificationRequest(payload, signature, eventType, headers);

        try
        {
            var result = await _paymentGatewayOrchestrator.VerifyAsync(normalizedProvider, verificationRequest, cancellationToken);
            var webhook = new PaymentWebhookRequest(
                Provider: normalizedProvider,
                EventType: result.EventType ?? eventType ?? string.Empty,
                ProviderReference: result.ProviderReference,
                Status: result.Status,
                Amount: result.Amount,
                Currency: result.Currency,
                Payload: result.RawPayload);

            var order = await _checkoutService.HandlePaymentWebhookAsync(webhook, cancellationToken);
            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized webhook for provider {Provider}", normalizedProvider);
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook payload for provider {Provider}", normalizedProvider);
            return BadRequest(ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse webhook payload for provider {Provider}", normalizedProvider);
            return BadRequest("Invalid payload format.");
        }
    }

    private static Dictionary<string, string> ToDictionary(IHeaderDictionary headers)
    {
        return headers.ToDictionary(
            kvp => kvp.Key,
            kvp => string.Join(',', kvp.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> ReadPayloadAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    private string? ResolveSignature(string provider)
    {
        if (Request.Headers.TryGetValue("X-Payment-Signature", out var signature))
        {
            return signature.FirstOrDefault();
        }

        if (provider.Equals("stripe", StringComparison.OrdinalIgnoreCase) && Request.Headers.TryGetValue("Stripe-Signature", out signature))
        {
            return signature.FirstOrDefault();
        }

        if (provider.Equals("paypal", StringComparison.OrdinalIgnoreCase) && Request.Headers.TryGetValue("PayPal-Transmission-Sig", out signature))
        {
            return signature.FirstOrDefault();
        }

        if (provider.Equals("razorpay", StringComparison.OrdinalIgnoreCase) && Request.Headers.TryGetValue("X-Razorpay-Signature", out signature))
        {
            return signature.FirstOrDefault();
        }

        return null;
    }

    private string? ResolveEventType()
    {
        if (Request.Headers.TryGetValue("X-Payment-Event", out var eventType))
        {
            return eventType.FirstOrDefault();
        }

        if (Request.Headers.TryGetValue("Stripe-Event", out eventType))
        {
            return eventType.FirstOrDefault();
        }

        if (Request.Headers.TryGetValue("PayPal-Transmission-Id", out eventType))
        {
            return eventType.FirstOrDefault();
        }

        if (Request.Headers.TryGetValue("X-Razorpay-Event", out eventType))
        {
            return eventType.FirstOrDefault();
        }

        return null;
    }
}
