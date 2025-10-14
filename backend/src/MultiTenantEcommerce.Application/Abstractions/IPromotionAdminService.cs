using MultiTenantEcommerce.Application.Models.Promotions;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPromotionAdminService
{
    Task<IReadOnlyList<CouponDto>> GetCouponsAsync(CancellationToken cancellationToken = default);
    Task<CouponDto> CreateCouponAsync(CreateCouponRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken = default);
    Task<PromotionCampaignDto> CreateCampaignAsync(CreatePromotionCampaignRequest request, CancellationToken cancellationToken = default);
    Task<PromotionCampaignDto?> UpdateCampaignStatusAsync(Guid campaignId, UpdatePromotionCampaignStatusRequest request, CancellationToken cancellationToken = default);
}
