using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ITaxService
{
    Task<TaxCalculationResult> CalculateAsync(decimal taxableAmount, decimal shippingAmount, CheckoutShippingAddressDto address, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<TaxRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
