using MultiTenantEcommerce.Application.Models.Reviews;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IProductReviewService
{
    Task<ProductReviewDto> SubmitAsync(SubmitProductReviewRequest request, CancellationToken cancellationToken = default);
    Task<ProductReviewDto?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<ProductReviewListResult> GetAsync(ProductReviewListQuery query, CancellationToken cancellationToken = default);
    Task<ProductReviewDto?> UpdateAsync(Guid reviewId, UpdateProductReviewRequest request, CancellationToken cancellationToken = default);
}
