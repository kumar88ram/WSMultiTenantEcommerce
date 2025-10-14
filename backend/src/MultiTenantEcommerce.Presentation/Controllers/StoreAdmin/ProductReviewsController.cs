using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Reviews;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("api/store-admin/product-reviews")]
[Authorize]
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _productReviewService;

    public ProductReviewsController(IProductReviewService productReviewService)
    {
        _productReviewService = productReviewService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductReviewListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews([FromQuery] ReviewQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = new ProductReviewListQuery(
            parameters.Page,
            parameters.PageSize,
            parameters.ProductId,
            parameters.IsApproved,
            parameters.IncludeUnapproved);

        var reviews = await _productReviewService.GetAsync(query, cancellationToken);
        return Ok(reviews);
    }

    [HttpGet("{reviewId:guid}")]
    [ProducesResponseType(typeof(ProductReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview(Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await _productReviewService.GetByIdAsync(reviewId, cancellationToken);
        return review is null ? NotFound() : Ok(review);
    }

    [HttpPut("{reviewId:guid}")]
    [ProducesResponseType(typeof(ProductReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateProductReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var review = await _productReviewService.UpdateAsync(reviewId, request, cancellationToken);
            return review is null ? NotFound() : Ok(review);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class ReviewQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public Guid? ProductId { get; set; }
        public bool? IsApproved { get; set; }
        public bool IncludeUnapproved { get; set; } = true;
    }
}
