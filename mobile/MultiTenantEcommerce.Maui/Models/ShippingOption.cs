namespace MultiTenantEcommerce.Maui.Models;

public class ShippingOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public string DisplayEstimate => $"{EstimatedDuration.TotalDays:0} day shipping";
}
