namespace MultiTenantEcommerce.Domain.Entities;

public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid TenantId { get; set; }
}
