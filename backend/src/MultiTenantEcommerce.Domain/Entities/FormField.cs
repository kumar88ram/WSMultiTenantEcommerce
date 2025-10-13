namespace MultiTenantEcommerce.Domain.Entities;

public class FormField : BaseEntity
{
    public Guid FormDefinitionId { get; set; }
    public FormDefinition FormDefinition { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? Placeholder { get; set; }
    public string? OptionsJson { get; set; }
}
