namespace MultiTenantEcommerce.Application.Models.Reviews;

public record SubmitProductReviewRequest(
    Guid ProductId,
    int Rating,
    string Comment,
    string? ReviewerName,
    string? ReviewerEmail,
    bool? IsFlagged = null);

public record UpdateProductReviewRequest(
    int? Rating = null,
    string? Comment = null,
    bool? IsApproved = null,
    bool? IsFlagged = null);

public record ProductReviewDto(
    Guid Id,
    Guid ProductId,
    int Rating,
    string Comment,
    bool IsApproved,
    bool IsFlagged,
    string? ReviewerName,
    string? ReviewerEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record ProductReviewListQuery(
    int Page = 1,
    int PageSize = 25,
    Guid? ProductId = null,
    bool? IsApproved = null,
    bool IncludeUnapproved = false);

public record ProductReviewListResult(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ProductReviewDto> Items);
