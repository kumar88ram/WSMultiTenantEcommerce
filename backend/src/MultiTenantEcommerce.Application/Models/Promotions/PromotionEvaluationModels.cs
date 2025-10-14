namespace MultiTenantEcommerce.Application.Models.Promotions;

public record PromotionItemContext(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyCollection<Guid> CategoryIds)
{
    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
}

public record PromotionEvaluationContext(
    Guid TenantId,
    IReadOnlyList<PromotionItemContext> Items,
    decimal Subtotal,
    string Currency,
    string? CouponCode);

public record PromotionBreakdown(string Source, string? Reference, decimal Amount);

public record PromotionEvaluationResult(
    decimal DiscountAmount,
    IReadOnlyList<PromotionBreakdown> Breakdown,
    Guid? CouponId,
    string? CouponCode,
    Guid? PromotionCampaignId,
    string? PromotionName);
