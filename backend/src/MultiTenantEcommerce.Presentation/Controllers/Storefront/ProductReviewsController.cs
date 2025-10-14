using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Reviews;

namespace MultiTenantEcommerce.Presentation.Controllers.Storefront;

[ApiController]
[Route("store/{tenant}/products/{productId:guid}/reviews")]
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _productReviewService;
    private readonly ITenantResolver _tenantResolver;

    public ProductReviewsController(IProductReviewService productReviewService, ITenantResolver tenantResolver)
    {
        _productReviewService = productReviewService;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductReviewListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews(string tenant, Guid productId, [FromQuery] ReviewQueryParameters parameters, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var query = new ProductReviewListQuery(
            parameters.Page,
            parameters.PageSize,
            productId,
            true,
            includeUnapproved: false);

        var reviews = await _productReviewService.GetAsync(query, cancellationToken);
        return Ok(reviews);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductReviewDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitReview(string tenant, Guid productId, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        var reviewRequest = new SubmitProductReviewRequest(
            productId,
            request.Rating,
            request.Comment,
            request.ReviewerName,
            request.ReviewerEmail,
            request.IsFlagged);

        var review = await _productReviewService.SubmitAsync(reviewRequest, cancellationToken);
        return CreatedAtAction(nameof(GetReviews), new { tenant, productId, page = 1, pageSize = 25 }, review);
    }

    private void EnsureTenantContext(string tenant)
    {
        if (!string.IsNullOrWhiteSpace(_tenantResolver.TenantIdentifier) &&
            !string.Equals(_tenantResolver.TenantIdentifier, tenant, StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["X-Tenant-Mismatch"] = _tenantResolver.TenantIdentifier;
        }
    }

    public class ReviewQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public record SubmitReviewRequest(int Rating, string Comment, string? ReviewerName, string? ReviewerEmail, bool? IsFlagged = null);
}
