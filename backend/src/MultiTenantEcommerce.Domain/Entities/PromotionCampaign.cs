namespace MultiTenantEcommerce.Domain.Entities;

public class PromotionCampaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
        = null;
    public CouponType Type { get; set; }
        = CouponType.Percentage;
    public CouponApplicability ApplyTo { get; set; }
        = CouponApplicability.Cart;
    public decimal Value { get; set; }
        = 0m;
    public Guid? TargetProductId { get; set; }
        = null;
    public Guid? TargetCategoryId { get; set; }
        = null;
    public decimal? MinimumOrderAmount { get; set; }
        = null;
    public DateTime StartsAt { get; set; }
        = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
        = null;
    public bool IsActive { get; set; }
        = true;
    public int Priority { get; set; }
        = 0;
}
