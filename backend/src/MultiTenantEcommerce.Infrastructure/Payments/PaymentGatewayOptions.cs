using System;
using System.Collections.Generic;

namespace MultiTenantEcommerce.Infrastructure.Payments;

public class PaymentGatewayOptions
{
    public IDictionary<string, PaymentProviderOption> Providers { get; set; } =
        new Dictionary<string, PaymentProviderOption>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, TenantPaymentOption> Tenants { get; set; } =
        new Dictionary<string, TenantPaymentOption>(StringComparer.OrdinalIgnoreCase);
}

public class PaymentProviderOption
{
    public string ProviderCurrency { get; set; } = "USD";
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public IDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class TenantPaymentOption
{
    public string DefaultCurrency { get; set; } = "USD";
    public IDictionary<string, decimal> ConversionRates { get; set; } =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
