using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Promotions;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class PromotionEngine : IPromotionEngine
{
    private readonly ApplicationDbContext _dbContext;

    public PromotionEngine(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PromotionEvaluationResult> EvaluateAsync(PromotionEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(context.CouponCode))
        {
            coupon = await _dbContext.Coupons
                .FirstOrDefaultAsync(
                    c => c.TenantId == context.TenantId && c.Code == context.CouponCode,
                    cancellationToken);

            if (coupon is not null && !IsCouponEligible(coupon, context.Subtotal, now))
            {
                coupon = null;
            }
        }

        var campaigns = await _dbContext.PromotionCampaigns
            .AsNoTracking()
            .Where(c => c.TenantId == context.TenantId && c.IsActive && c.StartsAt <= now)
            .Where(c => !c.EndsAt.HasValue || c.EndsAt.Value >= now)
            .OrderByDescending(c => c.Priority)
            .ToListAsync(cancellationToken);

        PromotionApplication? best = null;
        if (coupon is not null)
        {
            var couponApplication = EvaluateRule(
                coupon.Type,
                coupon.ApplyTo,
                coupon.Value,
                coupon.MinimumOrderAmount,
                coupon.TargetProductId,
                coupon.TargetCategoryId,
                context,
                source: "coupon",
                reference: coupon.Code);

            if (couponApplication is not null && couponApplication.DiscountAmount > 0)
            {
                best = couponApplication with { Coupon = coupon };
            }
        }

        foreach (var campaign in campaigns)
        {
            var campaignApplication = EvaluateRule(
                campaign.Type,
                campaign.ApplyTo,
                campaign.Value,
                campaign.MinimumOrderAmount,
                campaign.TargetProductId,
                campaign.TargetCategoryId,
                context,
                source: "campaign",
                reference: campaign.Name);

            if (campaignApplication is null || campaignApplication.DiscountAmount <= 0)
            {
                continue;
            }

            var shouldReplace = best is null || campaignApplication.DiscountAmount > best.DiscountAmount;
            if (shouldReplace)
            {
                best = campaignApplication with { Campaign = campaign };
            }
        }

        return best is null
            ? new PromotionEvaluationResult(0m, Array.Empty<PromotionBreakdown>(), null, context.CouponCode, null, null)
            : new PromotionEvaluationResult(
                best.DiscountAmount,
                new[] { best.Breakdown },
                best.Coupon?.Id,
                best.Coupon?.Code,
                best.Campaign?.Id,
                best.Campaign?.Name);
    }

    private static bool IsCouponEligible(Coupon coupon, decimal subtotal, DateTime now)
    {
        if (!coupon.IsActive)
        {
            return false;
        }

        if (coupon.StartsAt.HasValue && coupon.StartsAt.Value > now)
        {
            return false;
        }

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < now)
        {
            return false;
        }

        if (coupon.MinimumOrderAmount.HasValue && subtotal < coupon.MinimumOrderAmount.Value)
        {
            return false;
        }

        if (coupon.UsageLimit.HasValue && coupon.TimesRedeemed >= coupon.UsageLimit.Value)
        {
            return false;
        }

        return true;
    }

    private static PromotionApplication? EvaluateRule(
        CouponType type,
        CouponApplicability applyTo,
        decimal value,
        decimal? minimumOrder,
        Guid? targetProductId,
        Guid? targetCategoryId,
        PromotionEvaluationContext context,
        string source,
        string? reference)
    {
        if (minimumOrder.HasValue && context.Subtotal < minimumOrder.Value)
        {
            return null;
        }

        decimal applicableAmount = applyTo switch
        {
            CouponApplicability.Cart => context.Subtotal,
            CouponApplicability.Product when targetProductId.HasValue => context.Items
                .Where(i => i.ProductId == targetProductId.Value)
                .Sum(i => i.LineTotal),
            CouponApplicability.Category when targetCategoryId.HasValue => context.Items
                .Where(i => i.CategoryIds.Contains(targetCategoryId.Value))
                .Sum(i => i.LineTotal),
            _ => 0m
        };

        if (applicableAmount <= 0)
        {
            return null;
        }

        var discount = type switch
        {
            CouponType.Percentage => Math.Round(applicableAmount * (value / 100m), 2, MidpointRounding.AwayFromZero),
            CouponType.FixedAmount => Math.Min(value, applicableAmount),
            _ => 0m
        };

        if (discount <= 0)
        {
            return null;
        }

        return new PromotionApplication(discount, new PromotionBreakdown(source, reference, discount));
    }

    private sealed record PromotionApplication(
        decimal DiscountAmount,
        PromotionBreakdown Breakdown)
    {
        public Coupon? Coupon { get; init; }
        public PromotionCampaign? Campaign { get; init; }
    }
}
