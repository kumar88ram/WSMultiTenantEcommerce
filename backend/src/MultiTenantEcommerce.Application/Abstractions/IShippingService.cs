using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IShippingService
{
    Task<IReadOnlyList<ShippingZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default);
    Task<ShippingZoneDto?> GetZoneByIdAsync(Guid zoneId, CancellationToken cancellationToken = default);
    Task<ShippingMethodDetailDto?> GetMethodByIdAsync(Guid methodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CheckoutShippingMethodDto>> GetCheckoutMethodsAsync(CheckoutShippingAddressDto? address, CancellationToken cancellationToken = default);
    Task<ShippingQuoteDto> QuoteAsync(Guid shippingMethodId, CheckoutShippingAddressDto address, IReadOnlyCollection<CartItem> cartItems, CancellationToken cancellationToken = default);
}
