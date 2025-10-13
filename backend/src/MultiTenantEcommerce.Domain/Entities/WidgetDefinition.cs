namespace MultiTenantEcommerce.Domain.Entities;

public class WidgetDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
}
