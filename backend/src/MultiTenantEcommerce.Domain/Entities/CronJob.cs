namespace MultiTenantEcommerce.Domain.Entities;

public class CronJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ScheduleExpression { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
