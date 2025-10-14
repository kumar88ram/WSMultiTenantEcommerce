using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Promotions;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/promotions")]
[Authorize(Roles = "StoreAdmin")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionAdminService _promotionAdminService;

    public PromotionsController(IPromotionAdminService promotionAdminService)
    {
        _promotionAdminService = promotionAdminService;
    }

    [HttpGet("coupons")]
    [ProducesResponseType(typeof(IEnumerable<CouponDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCoupons(CancellationToken cancellationToken)
    {
        var coupons = await _promotionAdminService.GetCouponsAsync(cancellationToken);
        return Ok(coupons);
    }

    [HttpPost("coupons")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request, CancellationToken cancellationToken)
    {
        var coupon = await _promotionAdminService.CreateCouponAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCoupons), new { id = coupon.Id }, coupon);
    }

    [HttpGet("campaigns")]
    [ProducesResponseType(typeof(IEnumerable<PromotionCampaignDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaigns(CancellationToken cancellationToken)
    {
        var campaigns = await _promotionAdminService.GetCampaignsAsync(cancellationToken);
        return Ok(campaigns);
    }

    [HttpPost("campaigns")]
    [ProducesResponseType(typeof(PromotionCampaignDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCampaign([FromBody] CreatePromotionCampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = await _promotionAdminService.CreateCampaignAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCampaigns), new { id = campaign.Id }, campaign);
    }

    [HttpPatch("campaigns/{id:guid}")]
    [ProducesResponseType(typeof(PromotionCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCampaignStatus(Guid id, [FromBody] UpdatePromotionCampaignStatusRequest request, CancellationToken cancellationToken)
    {
        var campaign = await _promotionAdminService.UpdateCampaignStatusAsync(id, request, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        return Ok(campaign);
    }
}
