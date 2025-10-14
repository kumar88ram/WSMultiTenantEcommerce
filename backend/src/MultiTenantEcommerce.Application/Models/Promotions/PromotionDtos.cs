using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Promotions;

public record CouponDto(
    Guid Id,
    string Code,
    CouponType Type,
    CouponApplicability ApplyTo,
    decimal Value,
    Guid? TargetProductId,
    Guid? TargetCategoryId,
    int? UsageLimit,
    int TimesRedeemed,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    decimal? MinimumOrderAmount,
    bool IsActive);

public record PromotionCampaignDto(
    Guid Id,
    string Name,
    string? Description,
    CouponType Type,
    CouponApplicability ApplyTo,
    decimal Value,
    Guid? TargetProductId,
    Guid? TargetCategoryId,
    decimal? MinimumOrderAmount,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool IsActive,
    int Priority);

public record CreateCouponRequest(
    string Code,
    CouponType Type,
    CouponApplicability ApplyTo,
    decimal Value,
    Guid? TargetProductId,
    Guid? TargetCategoryId,
    int? UsageLimit,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    decimal? MinimumOrderAmount,
    bool IsActive);

public record CreatePromotionCampaignRequest(
    string Name,
    string? Description,
    CouponType Type,
    CouponApplicability ApplyTo,
    decimal Value,
    Guid? TargetProductId,
    Guid? TargetCategoryId,
    decimal? MinimumOrderAmount,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool IsActive,
    int Priority);

public record UpdatePromotionCampaignStatusRequest(bool IsActive);
