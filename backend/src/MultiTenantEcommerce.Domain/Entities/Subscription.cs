namespace MultiTenantEcommerce.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingReference { get; set; } = string.Empty;
}
