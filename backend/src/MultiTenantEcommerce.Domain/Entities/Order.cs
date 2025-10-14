using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class Order : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? GuestToken { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "USD";
    public string Email { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new Collection<OrderItem>();
    public ICollection<PaymentTransaction> Payments { get; set; } = new Collection<PaymentTransaction>();
}
