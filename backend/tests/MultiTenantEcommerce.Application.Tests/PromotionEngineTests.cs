using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Promotions;
using MultiTenantEcommerce.Application.Services;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantEcommerce.Application.Tests;

public class PromotionEngineTests
{
    private static ApplicationDbContext CreateDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"promotions_{Guid.NewGuid():N}")
            .Options;

        var context = new ApplicationDbContext(options, new TestTenantResolver(tenantId), null);
        return context;
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsCouponDiscount_WhenPercentageCouponMatches()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(tenantId);
        dbContext.Coupons.Add(new Coupon
        {
            TenantId = tenantId,
            Code = "SAVE20",
            Type = CouponType.Percentage,
            ApplyTo = CouponApplicability.Cart,
            Value = 20m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var engine = new PromotionEngine(dbContext);
        var context = new PromotionEvaluationContext(
            tenantId,
            new List<PromotionItemContext>
            {
                new(Guid.NewGuid(), null, 1, 100m, Array.Empty<Guid>())
            },
            100m,
            "USD",
            "SAVE20");

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(20m, result.DiscountAmount);
        Assert.Equal("SAVE20", result.CouponCode);
    }

    [Fact]
    public async Task EvaluateAsync_PrefersHigherValueCampaignOverCoupon()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(tenantId);
        var productId = Guid.NewGuid();

        dbContext.Coupons.Add(new Coupon
        {
            TenantId = tenantId,
            Code = "SAVE5",
            Type = CouponType.FixedAmount,
            ApplyTo = CouponApplicability.Cart,
            Value = 5m,
            IsActive = true
        });

        dbContext.PromotionCampaigns.Add(new PromotionCampaign
        {
            TenantId = tenantId,
            Name = "Spring sale",
            Type = CouponType.FixedAmount,
            ApplyTo = CouponApplicability.Cart,
            Value = 15m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var engine = new PromotionEngine(dbContext);
        var context = new PromotionEvaluationContext(
            tenantId,
            new List<PromotionItemContext>
            {
                new(productId, null, 1, 25m, Array.Empty<Guid>())
            },
            25m,
            "USD",
            "SAVE5");

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(15m, result.DiscountAmount);
        Assert.Null(result.CouponCode);
        Assert.NotNull(result.PromotionCampaignId);
    }

    [Fact]
    public async Task EvaluateAsync_IgnoresExpiredCoupon()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(tenantId);
        dbContext.Coupons.Add(new Coupon
        {
            TenantId = tenantId,
            Code = "OLD",
            Type = CouponType.FixedAmount,
            ApplyTo = CouponApplicability.Cart,
            Value = 50m,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var engine = new PromotionEngine(dbContext);
        var context = new PromotionEvaluationContext(
            tenantId,
            new List<PromotionItemContext>
            {
                new(Guid.NewGuid(), null, 1, 100m, Array.Empty<Guid>())
            },
            100m,
            "USD",
            "OLD");

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Null(result.CouponCode);
    }

    [Fact]
    public async Task EvaluateAsync_AppliesCategoryCampaign()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(tenantId);
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        dbContext.PromotionCampaigns.Add(new PromotionCampaign
        {
            TenantId = tenantId,
            Name = "Category boost",
            Type = CouponType.Percentage,
            ApplyTo = CouponApplicability.Category,
            Value = 30m,
            TargetCategoryId = categoryId,
            StartsAt = DateTime.UtcNow.AddHours(-2),
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var engine = new PromotionEngine(dbContext);
        var context = new PromotionEvaluationContext(
            tenantId,
            new List<PromotionItemContext>
            {
                new(productId, null, 1, 50m, new[] { categoryId }),
                new(Guid.NewGuid(), null, 1, 50m, Array.Empty<Guid>())
            },
            100m,
            "USD",
            null);

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(15m, result.DiscountAmount);
        Assert.Equal("Category boost", result.PromotionName);
    }

    private sealed class TestTenantResolver : ITenantResolver
    {
        public TestTenantResolver(Guid tenantId)
        {
            CurrentTenantId = tenantId;
        }

        public Guid CurrentTenantId { get; set; }
        public string? TenantIdentifier { get; set; }
        public string? ConnectionString { get; set; }
    }
}
