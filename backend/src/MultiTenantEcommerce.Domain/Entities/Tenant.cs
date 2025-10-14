namespace MultiTenantEcommerce.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string DbConnectionString { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? DefaultMetaTitle { get; set; }
    public string? DefaultMetaDescription { get; set; }
    public string? DefaultMetaKeywords { get; set; }
    public string? DefaultOgImageUrl { get; set; }
}
