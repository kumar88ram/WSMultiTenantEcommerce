namespace MultiTenantEcommerce.Domain.Entities;

public class FormDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
}
