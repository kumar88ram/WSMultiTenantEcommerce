namespace MultiTenantEcommerce.Maui.Models;

public class Product
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public double Rating { get; init; }
    public int RatingCount { get; init; }
    public string CategoryId { get; init; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ProductVariant> Variants { get; init; } = Array.Empty<ProductVariant>();
    public bool IsFeatured { get; init; }
    public bool IsWishlisted { get; set; }
    public string ShortDescription => Description.Length > 80 ? Description[..80] + "…" : Description;
}
