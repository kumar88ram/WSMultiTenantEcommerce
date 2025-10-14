using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public enum CouponType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum CouponApplicability
{
    Cart = 1,
    Product = 2,
    Category = 3
}

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
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
    public int? UsageLimit { get; set; }
        = null;
    public int TimesRedeemed { get; set; }
        = 0;
    public DateTime? StartsAt { get; set; }
        = null;
    public DateTime? ExpiresAt { get; set; }
        = null;
    public decimal? MinimumOrderAmount { get; set; }
        = null;
    public bool IsActive { get; set; }
        = true;

    public ICollection<Order> Orders { get; set; } = new Collection<Order>();
}
