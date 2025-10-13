namespace MultiTenantEcommerce.Domain.Entities;

public class StoreSetting : BaseEntity
{
    public string? LogoUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
}
