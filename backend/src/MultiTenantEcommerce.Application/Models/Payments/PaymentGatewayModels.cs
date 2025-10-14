using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Payments;

public record PaymentGatewayContext(
    Guid TenantId,
    string OrderCurrency,
    IReadOnlyDictionary<string, decimal> CurrencyConversionRates,
    IReadOnlyDictionary<string, string> TenantMetadata,
    PaymentProviderSettings ProviderSettings)
{
    public static PaymentGatewayContext Empty(Guid tenantId) => new(
        tenantId,
        "USD",
        new ReadOnlyDictionary<string, decimal>(new Dictionary<string, decimal> { { "USD", 1m } }),
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
        new PaymentProviderSettings("USD", string.Empty, string.Empty, string.Empty,
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())));
}

public record PaymentProviderSettings(
    string ProviderCurrency,
    string PublishableKey,
    string SecretKey,
    string WebhookSecret,
    IReadOnlyDictionary<string, string> Metadata);

public record PaymentVerificationRequest(
    string Payload,
    string? Signature,
    string? EventType,
    IReadOnlyDictionary<string, string> Headers);

public record PaymentVerificationResult(
    string ProviderReference,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string? EventType,
    string RawPayload);

public record PaymentRefundRequest(
    string ProviderReference,
    decimal Amount,
    string Currency,
    string? Reason);
