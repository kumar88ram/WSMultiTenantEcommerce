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

public class PayPalPaymentGateway : IPaymentGateway
{
    private readonly ILogger<PayPalPaymentGateway> _logger;

    public PayPalPaymentGateway(ILogger<PayPalPaymentGateway> logger)
    {
        _logger = logger;
    }

    public string Provider => "paypal";

    public Task<PaymentIntentDto> PayAsync(Order order, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        var providerReference = $"PAY-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Creating PayPal order for {OrderNumber} ({Amount} {Currency})",
            order.OrderNumber,
            order.GrandTotal,
            order.Currency);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerReference"] = providerReference,
            ["redirectUrl"] = $"https://www.paypal.test/checkoutnow?token={providerReference}",
            ["providerCurrency"] = context.ProviderSettings.ProviderCurrency,
            ["orderCurrency"] = order.Currency
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
            paymentUrl: metadata["redirectUrl"],
            Metadata: metadata);

        return Task.FromResult(paymentIntent);
    }

    public Task<PaymentVerificationResult> VerifyAsync(PaymentVerificationRequest request, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        ValidateSignature(request, context.ProviderSettings.WebhookSecret);

        using var document = JsonDocument.Parse(request.Payload);
        var root = document.RootElement;
        var resource = root.TryGetProperty("resource", out var resourceElement) ? resourceElement : root;

        var reference = TryGetString(resource, "id") ?? string.Empty;
        var statusText = TryGetString(resource, "status") ?? "pending";
        var amount = TryGetAmount(resource) ?? 0m;
        var currency = TryGetCurrency(resource) ?? context.ProviderSettings.ProviderCurrency;
        var eventType = request.EventType ?? TryGetString(root, "event_type");

        var status = statusText.ToLowerInvariant() switch
        {
            "completed" or "captured" => PaymentStatus.Captured,
            "approved" => PaymentStatus.Authorized,
            "pending" => PaymentStatus.Pending,
            "denied" or "failed" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };

        _logger.LogInformation("PayPal webhook received for {Reference} with status {Status}", reference, status);

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
            "Simulating PayPal refund for {Reference} with amount {Amount} {Currency}",
            request.ProviderReference,
            request.Amount,
            request.Currency);
        return Task.FromResult(PaymentStatus.Refunded);
    }

    private static void ValidateSignature(PaymentVerificationRequest request, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Webhook secret is not configured for PayPal.");
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
            throw new UnauthorizedAccessException("Invalid PayPal webhook signature.");
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

    private static decimal? TryGetAmount(JsonElement element)
    {
        if (element.TryGetProperty("amount", out var amountElement))
        {
            if (amountElement.ValueKind == JsonValueKind.Object)
            {
                if (amountElement.TryGetProperty("value", out var value) && value.TryGetDecimal(out var decimalValue))
                {
                    return decimalValue;
                }
            }
            else if (amountElement.ValueKind == JsonValueKind.Number && amountElement.TryGetDecimal(out var numberValue))
            {
                return numberValue;
            }
        }

        return null;
    }

    private static string? TryGetCurrency(JsonElement element)
    {
        if (element.TryGetProperty("amount", out var amountElement) && amountElement.ValueKind == JsonValueKind.Object)
        {
            if (amountElement.TryGetProperty("currency_code", out var code) && code.ValueKind == JsonValueKind.String)
            {
                return code.GetString();
            }
        }

        if (element.TryGetProperty("currency", out var currencyElement) && currencyElement.ValueKind == JsonValueKind.String)
        {
            return currencyElement.GetString();
        }

        return null;
    }
}
