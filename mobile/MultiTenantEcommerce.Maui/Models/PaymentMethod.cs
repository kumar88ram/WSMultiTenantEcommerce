namespace MultiTenantEcommerce.Maui.Models;

public class PaymentMethod
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}
