namespace MultiTenantEcommerce.Domain.Entities;

public class ProductReview : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewerEmail { get; set; }
    public bool IsFlagged { get; set; }
}
