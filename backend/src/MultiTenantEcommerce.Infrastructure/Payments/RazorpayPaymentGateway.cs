using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Payments;

public class RazorpayPaymentGateway : IPaymentGateway
{
    private readonly ILogger<RazorpayPaymentGateway> _logger;

    public RazorpayPaymentGateway(ILogger<RazorpayPaymentGateway> logger)
    {
        _logger = logger;
    }

    public string Provider => "razorpay";

    public Task<PaymentIntentDto> PayAsync(Order order, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        var providerReference = $"order_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Creating Razorpay order for {OrderNumber} ({Amount} {Currency})",
            order.OrderNumber,
            order.GrandTotal,
            order.Currency);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerReference"] = providerReference,
            ["providerCurrency"] = context.ProviderSettings.ProviderCurrency,
            ["orderCurrency"] = order.Currency,
            ["callbackUrl"] = $"https://api.razorpay.test/v1/checkout/{providerReference}"
        };

        foreach (var kvp in context.CurrencyConversionRates)
        {
            metadata[$"conversion:{kvp.Key}"] = kvp.Value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        foreach (var kvp in context.TenantMetadata)
        {
            metadata[$"tenant:{kvp.Key}"] = kvp.Value;
        }

        foreach (var kvp in context.ProviderSettings.Metadata)
        {
            metadata[$"provider:{kvp.Key}"] = kvp.Value;
        }

        var paymentIntent = new PaymentIntentDto(
            Provider,
            clientSecret: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            paymentUrl: metadata["callbackUrl"],
            Metadata: metadata);

        return Task.FromResult(paymentIntent);
    }

    public Task<PaymentVerificationResult> VerifyAsync(PaymentVerificationRequest request, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        ValidateSignature(request, context.ProviderSettings.WebhookSecret);

        using var document = JsonDocument.Parse(request.Payload);
        var root = document.RootElement;
        var payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : root;
        var payment = payload.TryGetProperty("payment", out var paymentElement) ? paymentElement : payload;
        var entity = payment.TryGetProperty("entity", out var entityElement) ? entityElement : payment;

        var reference = TryGetString(entity, "order_id") ?? TryGetString(entity, "id") ?? string.Empty;
        var statusText = TryGetString(entity, "status") ?? "created";
        var amount = TryGetDecimal(entity, "amount") ?? 0m;
        if (amount > 0)
        {
            amount /= 100m; // Razorpay reports in subunits
        }
        var currency = TryGetString(entity, "currency") ?? context.ProviderSettings.ProviderCurrency;
        var eventType = request.EventType ?? TryGetString(root, "event");

        var status = statusText.ToLowerInvariant() switch
        {
            "captured" => PaymentStatus.Captured,
            "authorized" => PaymentStatus.Authorized,
            "failed" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };

        _logger.LogInformation("Razorpay webhook processed for {Reference} with status {Status}", reference, status);

        return Task.FromResult(new PaymentVerificationResult(
            reference,
            status,
            amount,
            currency,
            eventType,
            request.Payload));
    }

    public Task<PaymentStatus> RefundAsync(PaymentRefundRequest request, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Simulating Razorpay refund for {Reference} with amount {Amount} {Currency}",
            request.ProviderReference,
            request.Amount,
            request.Currency);
        return Task.FromResult(PaymentStatus.Refunded);
    }

    private static void ValidateSignature(PaymentVerificationRequest request, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Webhook secret is not configured for Razorpay.");
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            throw new UnauthorizedAccessException("Missing webhook signature header.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Payload));
        var expected = Convert.ToHexString(hash);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(request.Signature)))
        {
            throw new UnauthorizedAccessException("Invalid Razorpay webhook signature.");
        }
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.Number)
        {
            if (value.TryGetDecimal(out var result))
            {
                return result;
            }
        }

        return null;
    }
}
