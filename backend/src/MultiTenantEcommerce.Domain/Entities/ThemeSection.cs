namespace MultiTenantEcommerce.Domain.Entities;

public class ThemeSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThemeId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string JsonConfig { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Theme? Theme { get; set; }
}
