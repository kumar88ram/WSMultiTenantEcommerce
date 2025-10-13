namespace MultiTenantEcommerce.Domain.Entities;

public class ProductVariantAttributeValue : BaseEntity
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public Guid AttributeValueId { get; set; }
    public AttributeValue AttributeValue { get; set; } = null!;
}
