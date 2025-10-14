using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Infrastructure.Shipping;

public class NullShippingCarrierAdapter : IShippingCarrierAdapter
{
    private readonly ILogger<NullShippingCarrierAdapter> _logger;

    public NullShippingCarrierAdapter(ILogger<NullShippingCarrierAdapter> logger)
    {
        _logger = logger;
    }

    public string CarrierKey => "manual";

    public Task<ShippingQuoteDto?> QuoteAsync(ShippingCarrierQuoteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No external carrier integration configured for key {CarrierKey}. Falling back to configured rates.", CarrierKey);
        return Task.FromResult<ShippingQuoteDto?>(null);
    }
}
