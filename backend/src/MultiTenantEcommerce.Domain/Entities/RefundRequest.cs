using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class RefundRequest : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Pending;
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? DecisionNotes { get; set; }
    public DateTime? DecisionAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
    public ICollection<RefundRequestItem> Items { get; set; } = new Collection<RefundRequestItem>();
}
