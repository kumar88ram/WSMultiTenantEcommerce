using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record SubscriptionRequest(
    Guid TenantId,
    Guid PlanId,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime? NextBillingDate,
    decimal Amount,
    string Currency,
    string BillingReference
);

public record SubscriptionResponse(
    Guid Id,
    Guid TenantId,
    Guid PlanId,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    DateTime? NextBillingDate,
    decimal Amount,
    string Currency,
    string BillingReference
)
{
    public static SubscriptionResponse FromEntity(Subscription subscription) => new(
        subscription.Id,
        subscription.TenantId,
        subscription.PlanId,
        subscription.Status,
        subscription.StartDate,
        subscription.EndDate,
        subscription.NextBillingDate,
        subscription.Amount,
        subscription.Currency,
        subscription.BillingReference
    );
}
