namespace MultiTenantEcommerce.Domain.Entities;

public class MenuDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string TreeJson { get; set; } = "{}";
}
