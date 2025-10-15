namespace MultiTenantEcommerce.Domain.Entities;

public class ThemeUsageAnalytics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThemeId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public bool IsActive { get; set; }
    public double TotalActiveDays { get; set; }
}
