namespace MultiTenantEcommerce.Domain.Entities;

public class AttributeValue : BaseEntity
{
    public Guid ProductAttributeId { get; set; }
    public ProductAttribute ProductAttribute { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<ProductVariantAttributeValue> VariantValues { get; set; } = new List<ProductVariantAttributeValue>();
}
