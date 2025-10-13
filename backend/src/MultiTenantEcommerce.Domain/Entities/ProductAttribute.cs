namespace MultiTenantEcommerce.Domain.Entities;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ICollection<AttributeValue> Values { get; set; } = new List<AttributeValue>();
}
