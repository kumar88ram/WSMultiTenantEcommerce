using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class ShippingZone : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public ICollection<ShippingZoneRegion> Regions { get; set; } = new Collection<ShippingZoneRegion>();
    public ICollection<ShippingMethod> Methods { get; set; } = new Collection<ShippingMethod>();
}
