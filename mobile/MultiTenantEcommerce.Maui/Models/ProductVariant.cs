namespace MultiTenantEcommerce.Maui.Models;

public class ProductVariant
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
    public string SelectedOption { get; set; } = string.Empty;
    public decimal PriceModifier { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int AvailableStock { get; init; }
}
