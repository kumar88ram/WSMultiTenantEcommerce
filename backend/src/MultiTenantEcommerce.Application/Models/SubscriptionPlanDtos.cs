using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record SubscriptionPlanRequest(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string BillingPeriod,
    PlanBillingMetadataDto BillingMetadata
);

public record PlanBillingMetadataDto(
    decimal? SetupFee,
    int? TrialPeriodDays,
    int BillingFrequency,
    string ExternalPlanId,
    int? GracePeriodDays
)
{
    public static PlanBillingMetadataDto FromEntity(PlanBillingMetadata metadata) => new(
        metadata.SetupFee,
        metadata.TrialPeriodDays,
        metadata.BillingFrequency,
        metadata.ExternalPlanId,
        metadata.GracePeriodDays
    );

    public PlanBillingMetadata ToEntity() => new()
    {
        SetupFee = SetupFee,
        TrialPeriodDays = TrialPeriodDays,
        BillingFrequency = BillingFrequency,
        ExternalPlanId = ExternalPlanId,
        GracePeriodDays = GracePeriodDays
    };
}

public record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string BillingPeriod,
    PlanBillingMetadataDto BillingMetadata
)
{
    public static SubscriptionPlanResponse FromEntity(SubscriptionPlan plan) => new(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.Price,
        plan.Currency,
        plan.BillingPeriod,
        PlanBillingMetadataDto.FromEntity(plan.BillingMetadata)
    );
}
