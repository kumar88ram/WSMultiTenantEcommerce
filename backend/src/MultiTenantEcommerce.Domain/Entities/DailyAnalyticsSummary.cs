namespace MultiTenantEcommerce.Domain.Entities;

public class DailyAnalyticsSummary : BaseEntity
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public int VisitCount { get; set; }
    public int OrderCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal ConversionRate { get; set; }
}
