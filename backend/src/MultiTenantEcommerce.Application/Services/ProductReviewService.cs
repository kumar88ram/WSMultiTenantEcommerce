using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Reviews;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class ProductReviewService : IProductReviewService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<ProductReviewService> _logger;

    public ProductReviewService(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        ILogger<ProductReviewService> logger)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public async Task<ProductReviewDto> SubmitAsync(SubmitProductReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException("Comment is required", nameof(request.Comment));
        }

        var tenantId = EnsureTenant();
        ValidateRating(request.Rating);

        var productExists = await _dbContext.Products.AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw new InvalidOperationException("Product not found");
        }

        var review = new ProductReview
        {
            TenantId = tenantId,
            ProductId = request.ProductId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            ReviewerName = string.IsNullOrWhiteSpace(request.ReviewerName) ? null : request.ReviewerName.Trim(),
            ReviewerEmail = string.IsNullOrWhiteSpace(request.ReviewerEmail) ? null : request.ReviewerEmail.Trim().ToLowerInvariant(),
            IsApproved = false,
            IsFlagged = request.IsFlagged ?? false
        };

        await _dbContext.ProductReviews.AddAsync(review, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} submitted for product {ProductId}", review.Id, review.ProductId);

        return Map(review);
    }

    public async Task<ProductReviewDto?> GetByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var review = await _dbContext.ProductReviews.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.TenantId == tenantId, cancellationToken);
        return review is null ? null : Map(review);
    }

    public async Task<ProductReviewListResult> GetAsync(ProductReviewListQuery query, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var reviewsQuery = _dbContext.ProductReviews
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (query.ProductId.HasValue)
        {
            reviewsQuery = reviewsQuery.Where(r => r.ProductId == query.ProductId.Value);
        }

        if (!query.IncludeUnapproved)
        {
            reviewsQuery = reviewsQuery.Where(r => r.IsApproved);
        }

        if (query.IsApproved.HasValue)
        {
            reviewsQuery = reviewsQuery.Where(r => r.IsApproved == query.IsApproved.Value);
        }

        var totalCount = await reviewsQuery.CountAsync(cancellationToken);

        var items = await reviewsQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto(
                r.Id,
                r.ProductId,
                r.Rating,
                r.Comment,
                r.IsApproved,
                r.IsFlagged,
                r.ReviewerName,
                r.ReviewerEmail,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new ProductReviewListResult(page, pageSize, totalCount, items);
    }

    public async Task<ProductReviewDto?> UpdateAsync(Guid reviewId, UpdateProductReviewRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenant();
        var review = await _dbContext.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.TenantId == tenantId, cancellationToken);

        if (review is null)
        {
            return null;
        }

        if (request.Rating.HasValue)
        {
            ValidateRating(request.Rating.Value);
            review.Rating = request.Rating.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            review.Comment = request.Comment.Trim();
        }

        if (request.IsApproved.HasValue)
        {
            review.IsApproved = request.IsApproved.Value;
        }

        if (request.IsFlagged.HasValue)
        {
            review.IsFlagged = request.IsFlagged.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} updated for product {ProductId}", review.Id, review.ProductId);

        return Map(review);
    }

    private Guid EnsureTenant()
    {
        if (_tenantResolver.CurrentTenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not resolved for product review operations.");
        }

        return _tenantResolver.CurrentTenantId;
    }

    private static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        }
    }

    private static ProductReviewDto Map(ProductReview review) => new(
        review.Id,
        review.ProductId,
        review.Rating,
        review.Comment,
        review.IsApproved,
        review.IsFlagged,
        review.ReviewerName,
        review.ReviewerEmail,
        review.CreatedAt,
        review.UpdatedAt);
}
