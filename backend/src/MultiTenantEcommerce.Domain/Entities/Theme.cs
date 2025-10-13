namespace MultiTenantEcommerce.Domain.Entities;

public class Theme : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? AppliedAt { get; set; }
}
