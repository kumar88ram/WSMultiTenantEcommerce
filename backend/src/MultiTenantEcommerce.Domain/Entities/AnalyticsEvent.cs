namespace MultiTenantEcommerce.Domain.Entities;

public class AnalyticsEvent : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public decimal? Amount { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? Metadata { get; set; }
}
