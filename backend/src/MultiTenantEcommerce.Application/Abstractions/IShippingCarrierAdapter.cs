using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IShippingCarrierAdapter
{
    string CarrierKey { get; }

    Task<ShippingQuoteDto?> QuoteAsync(ShippingCarrierQuoteRequest request, CancellationToken cancellationToken = default);
}
