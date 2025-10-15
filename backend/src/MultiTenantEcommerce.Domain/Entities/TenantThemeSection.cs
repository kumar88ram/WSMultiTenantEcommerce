namespace MultiTenantEcommerce.Domain.Entities;

public class TenantThemeSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantThemeId { get; set; }
    public Guid TenantId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string JsonConfig { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
