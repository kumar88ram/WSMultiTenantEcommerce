using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Payments;

public class PaymentGatewayOrchestrator : IPaymentGatewayOrchestrator
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly ITenantResolver _tenantResolver;
    private readonly IOptions<PaymentGatewayOptions> _options;
    private readonly ILogger<PaymentGatewayOrchestrator> _logger;

    public PaymentGatewayOrchestrator(
        IEnumerable<IPaymentGateway> gateways,
        ITenantResolver tenantResolver,
        IOptions<PaymentGatewayOptions> options,
        ILogger<PaymentGatewayOrchestrator> logger)
    {
        _gateways = gateways;
        _tenantResolver = tenantResolver;
        _options = options;
        _logger = logger;
    }

    public Task<PaymentIntentDto> PayAsync(string provider, Order order, CancellationToken cancellationToken = default)
    {
        var gateway = ResolveProvider(provider);
        var context = BuildContext(provider, order.Currency);
        return gateway.PayAsync(order, context, cancellationToken);
    }

    public Task<PaymentVerificationResult> VerifyAsync(string provider, PaymentVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var gateway = ResolveProvider(provider);
        var context = BuildContext(provider, null);
        return gateway.VerifyAsync(request, context, cancellationToken);
    }

    public Task<PaymentStatus> RefundAsync(string provider, PaymentRefundRequest request, CancellationToken cancellationToken = default)
    {
        var gateway = ResolveProvider(provider);
        var context = BuildContext(provider, request.Currency);
        return gateway.RefundAsync(request, context, cancellationToken);
    }

    private IPaymentGateway ResolveProvider(string provider)
    {
        var gateway = _gateways.FirstOrDefault(g => string.Equals(g.Provider, provider, StringComparison.OrdinalIgnoreCase));
        if (gateway is not null)
        {
            return gateway;
        }

        _logger.LogError("Payment gateway for provider {Provider} was not found", provider);
        throw new InvalidOperationException($"Payment gateway for provider '{provider}' was not registered.");
    }

    private PaymentGatewayContext BuildContext(string provider, string? orderCurrency)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        var tenantKey = _tenantResolver.TenantIdentifier;
        var options = _options.Value;

        TenantPaymentOption tenantOption;
        if (tenantKey is not null && options.Tenants.TryGetValue(tenantKey, out var specificTenantOption))
        {
            tenantOption = specificTenantOption;
        }
        else if (options.Tenants.TryGetValue(tenantId.ToString(), out var tenantIdOption))
        {
            tenantOption = tenantIdOption;
        }
        else if (options.Tenants.TryGetValue("default", out var defaultOption))
        {
            tenantOption = defaultOption;
        }
        else
        {
            tenantOption = new TenantPaymentOption();
        }

        if (!options.Providers.TryGetValue(provider, out var providerOption))
        {
            providerOption = new PaymentProviderOption();
        }

        var conversionRates = tenantOption.ConversionRates ?? new Dictionary<string, decimal>();
        var conversionDictionary = new Dictionary<string, decimal>(conversionRates, StringComparer.OrdinalIgnoreCase);
        if (orderCurrency is not null && !conversionDictionary.ContainsKey(orderCurrency))
        {
            conversionDictionary[orderCurrency] = 1m;
        }
        if (!conversionDictionary.ContainsKey(providerOption.ProviderCurrency))
        {
            conversionDictionary[providerOption.ProviderCurrency] = 1m;
        }

        var tenantMetadata = tenantOption.Metadata ?? new Dictionary<string, string>();
        var tenantMetadataDictionary = new Dictionary<string, string>(tenantMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["tenantId"] = tenantId.ToString()
        };
        if (!string.IsNullOrWhiteSpace(tenantKey))
        {
            tenantMetadataDictionary["tenantIdentifier"] = tenantKey;
        }

        var providerMetadata = providerOption.Metadata ?? new Dictionary<string, string>();
        var providerMetadataDictionary = new Dictionary<string, string>(providerMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = provider
        };

        return new PaymentGatewayContext(
            tenantId,
            orderCurrency ?? tenantOption.DefaultCurrency,
            new ReadOnlyDictionary<string, decimal>(conversionDictionary),
            new ReadOnlyDictionary<string, string>(tenantMetadataDictionary),
            new PaymentProviderSettings(
                providerOption.ProviderCurrency,
                providerOption.PublishableKey,
                providerOption.SecretKey,
                providerOption.WebhookSecret,
                new ReadOnlyDictionary<string, string>(providerMetadataDictionary)));
    }
}
