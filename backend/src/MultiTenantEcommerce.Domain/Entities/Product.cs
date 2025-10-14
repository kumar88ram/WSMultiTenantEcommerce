namespace MultiTenantEcommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<Inventory> Inventory { get; set; } = new List<Inventory>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}
