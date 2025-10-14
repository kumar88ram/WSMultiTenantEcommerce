namespace MultiTenantEcommerce.Domain.Entities;

public class ShippingRateTableEntry : BaseEntity
{
    public Guid ShippingMethodId { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }
    public decimal MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal Rate { get; set; }
}
