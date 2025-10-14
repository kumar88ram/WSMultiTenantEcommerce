using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Payments;

public class StripeLikePaymentGatewayClient : IPaymentGatewayClient
{
    private readonly ILogger<StripeLikePaymentGatewayClient> _logger;
    private readonly string _publishableKey;

    public StripeLikePaymentGatewayClient(IConfiguration configuration, ILogger<StripeLikePaymentGatewayClient> logger)
    {
        _logger = logger;
        _publishableKey = configuration.GetSection("Payments:StripeLike:PublishableKey").Value ?? "pk_test_sample";
    }

    public Task<PaymentIntentDto> CreatePaymentIntentAsync(Order order, CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, string>
        {
            ["orderNumber"] = order.OrderNumber,
            ["tenantId"] = order.TenantId.ToString(),
            ["email"] = order.Email
        };

        var clientSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var paymentUrl = $"https://payments.example.com/checkout?intent={clientSecret}&key={_publishableKey}";

        _logger.LogInformation(
            "Created payment intent for order {OrderNumber} ({Amount} {Currency})",
            order.OrderNumber,
            order.GrandTotal,
            order.Currency);

        return Task.FromResult(new PaymentIntentDto(
            Provider: "stripe-like",
            ClientSecret: clientSecret,
            PaymentUrl: paymentUrl,
            Metadata: metadata));
    }

    public PaymentStatus TranslateStatus(string providerStatus)
    {
        return providerStatus.ToLowerInvariant() switch
        {
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_capture" => PaymentStatus.Authorized,
            "succeeded" => PaymentStatus.Captured,
            "failed" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };
    }
}
