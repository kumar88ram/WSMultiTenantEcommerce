using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Promotions;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class PromotionAdminService : IPromotionAdminService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public PromotionAdminService(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    public async Task<IReadOnlyList<CouponDto>> GetCouponsAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await _dbContext.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return coupons.Select(MapCoupon).ToList();
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Coupon code is required", nameof(request));
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var tenantId = _tenantResolver.CurrentTenantId;
        var exists = await _dbContext.Coupons
            .AnyAsync(c => c.Code == normalizedCode && c.TenantId == tenantId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"A coupon with code '{normalizedCode}' already exists.");
        }

        var coupon = new Coupon
        {
            TenantId = tenantId,
            Code = normalizedCode,
            Type = request.Type,
            ApplyTo = request.ApplyTo,
            Value = request.Value,
            TargetProductId = request.TargetProductId,
            TargetCategoryId = request.TargetCategoryId,
            UsageLimit = request.UsageLimit,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            MinimumOrderAmount = request.MinimumOrderAmount,
            IsActive = request.IsActive
        };

        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCoupon(coupon);
    }

    public async Task<IReadOnlyList<PromotionCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var campaigns = await _dbContext.PromotionCampaigns
            .AsNoTracking()
            .OrderByDescending(c => c.Priority)
            .ThenByDescending(c => c.StartsAt)
            .ToListAsync(cancellationToken);

        return campaigns.Select(MapCampaign).ToList();
    }

    public async Task<PromotionCampaignDto> CreateCampaignAsync(CreatePromotionCampaignRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Campaign name is required", nameof(request));
        }

        if (request.EndsAt.HasValue && request.EndsAt < request.StartsAt)
        {
            throw new ArgumentException("Campaign end date must be after the start date", nameof(request));
        }

        var campaign = new PromotionCampaign
        {
            TenantId = _tenantResolver.CurrentTenantId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            ApplyTo = request.ApplyTo,
            Value = request.Value,
            TargetProductId = request.TargetProductId,
            TargetCategoryId = request.TargetCategoryId,
            MinimumOrderAmount = request.MinimumOrderAmount,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive,
            Priority = request.Priority
        };

        _dbContext.PromotionCampaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCampaign(campaign);
    }

    public async Task<PromotionCampaignDto?> UpdateCampaignStatusAsync(Guid campaignId, UpdatePromotionCampaignStatusRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.PromotionCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return null;
        }

        campaign.IsActive = request.IsActive;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCampaign(campaign);
    }

    private static CouponDto MapCoupon(Coupon coupon) => new(
        coupon.Id,
        coupon.Code,
        coupon.Type,
        coupon.ApplyTo,
        coupon.Value,
        coupon.TargetProductId,
        coupon.TargetCategoryId,
        coupon.UsageLimit,
        coupon.TimesRedeemed,
        coupon.StartsAt,
        coupon.ExpiresAt,
        coupon.MinimumOrderAmount,
        coupon.IsActive);

    private static PromotionCampaignDto MapCampaign(PromotionCampaign campaign) => new(
        campaign.Id,
        campaign.Name,
        campaign.Description,
        campaign.Type,
        campaign.ApplyTo,
        campaign.Value,
        campaign.TargetProductId,
        campaign.TargetCategoryId,
        campaign.MinimumOrderAmount,
        campaign.StartsAt,
        campaign.EndsAt,
        campaign.IsActive,
        campaign.Priority);
}
