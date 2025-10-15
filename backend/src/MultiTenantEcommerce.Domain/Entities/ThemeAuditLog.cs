namespace MultiTenantEcommerce.Domain.Entities;

public class ThemeAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThemeId { get; set; }
    public Guid AdminId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public Guid? TargetTenantId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
