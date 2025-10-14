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

public class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(ILogger<StripePaymentGateway> logger)
    {
        _logger = logger;
    }

    public string Provider => "stripe";

    public Task<PaymentIntentDto> PayAsync(Order order, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        var providerReference = $"pi_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Creating Stripe payment intent for order {OrderNumber} ({Amount} {Currency})",
            order.OrderNumber,
            order.GrandTotal,
            order.Currency);

        var metadata = BuildMetadata(context, providerReference);
        var paymentIntent = new PaymentIntentDto(
            Provider,
            clientSecret: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            paymentUrl: $"https://payments.stripe.test/pay/{providerReference}",
            Metadata: metadata);

        return Task.FromResult(paymentIntent);
    }

    public Task<PaymentVerificationResult> VerifyAsync(PaymentVerificationRequest request, PaymentGatewayContext context, CancellationToken cancellationToken = default)
    {
        ValidateSignature(request, context);

        using var document = JsonDocument.Parse(request.Payload);
        var root = document.RootElement;
        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : root;

        var reference = TryGetString(data, "reference") ?? TryGetString(data, "id") ?? string.Empty;
        var statusText = TryGetString(data, "status") ?? "pending";
        var amount = TryGetDecimal(data, "amount") ?? 0m;
        var currency = TryGetString(data, "currency") ?? context.ProviderSettings.ProviderCurrency;
        var eventType = request.EventType ?? TryGetString(root, "type");

        var status = statusText.ToLowerInvariant() switch
        {
            "succeeded" or "paid" => PaymentStatus.Captured,
            "requires_capture" or "authorized" => PaymentStatus.Authorized,
            "requires_payment_method" or "pending" => PaymentStatus.Pending,
            "failed" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };

        _logger.LogInformation(
            "Stripe webhook processed for {Reference} with status {Status}",
            reference,
            status);

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
            "Simulating Stripe refund for {Reference} with amount {Amount} {Currency}",
            request.ProviderReference,
            request.Amount,
            request.Currency);
        return Task.FromResult(PaymentStatus.Refunded);
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(PaymentGatewayContext context, string providerReference)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["providerReference"] = providerReference,
            ["providerCurrency"] = context.ProviderSettings.ProviderCurrency,
            ["orderCurrency"] = context.OrderCurrency
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

        return metadata;
    }

    private static void ValidateSignature(PaymentVerificationRequest request, PaymentGatewayContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ProviderSettings.WebhookSecret))
        {
            throw new InvalidOperationException("Webhook secret is not configured for Stripe.");
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            throw new UnauthorizedAccessException("Missing webhook signature header.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(context.ProviderSettings.WebhookSecret));
        var payloadBytes = Encoding.UTF8.GetBytes(request.Payload);
        var hash = hmac.ComputeHash(payloadBytes);
        var expectedSignature = Convert.ToHexString(hash);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(request.Signature)))
        {
            throw new UnauthorizedAccessException("Invalid Stripe webhook signature.");
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
