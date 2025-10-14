namespace MultiTenantEcommerce.Domain.Entities;

public static class AnalyticsEventType
{
    public const string Visit = "visit";
    public const string ProductViewed = "product_view";
    public const string CheckoutStarted = "checkout_started";
    public const string OrderCompleted = "order_completed";
}
