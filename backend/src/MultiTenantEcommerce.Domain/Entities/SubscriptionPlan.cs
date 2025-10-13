namespace MultiTenantEcommerce.Domain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingPeriod { get; set; } = "Monthly";
    public PlanBillingMetadata BillingMetadata { get; set; } = new();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public class PlanBillingMetadata
{
    public decimal? SetupFee { get; set; }
    public int? TrialPeriodDays { get; set; }
    public int BillingFrequency { get; set; } = 1;
    public string ExternalPlanId { get; set; } = string.Empty;
    public int? GracePeriodDays { get; set; }
}
