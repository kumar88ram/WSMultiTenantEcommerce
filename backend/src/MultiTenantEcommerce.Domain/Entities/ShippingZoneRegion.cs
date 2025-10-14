namespace MultiTenantEcommerce.Domain.Entities;

public class ShippingZoneRegion : BaseEntity
{
    public Guid ShippingZoneId { get; set; }
    public ShippingZone? ShippingZone { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? StateCode { get; set; }
}
