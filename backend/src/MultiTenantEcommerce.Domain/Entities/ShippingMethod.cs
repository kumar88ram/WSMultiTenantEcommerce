using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class ShippingMethod : BaseEntity
{
    public Guid ShippingZoneId { get; set; }
    public ShippingZone? ShippingZone { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShippingMethodType MethodType { get; set; }
    public ShippingRateConditionType RateConditionType { get; set; } = ShippingRateConditionType.None;
    public decimal? FlatRate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal? MinimumOrderTotal { get; set; }
    public decimal? MaximumOrderTotal { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? CarrierKey { get; set; }
    public string? CarrierServiceLevel { get; set; }
    public string? IntegrationSettingsJson { get; set; }
    public int? EstimatedTransitMinDays { get; set; }
    public int? EstimatedTransitMaxDays { get; set; }
    public ICollection<ShippingRateTableEntry> RateTable { get; set; } = new Collection<ShippingRateTableEntry>();
}
