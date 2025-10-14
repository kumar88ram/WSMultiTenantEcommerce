namespace MultiTenantEcommerce.Domain.Entities;

public class RefundRequestItem : BaseEntity
{
    public Guid RefundRequestId { get; set; }
    public RefundRequest? RefundRequest { get; set; }
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
