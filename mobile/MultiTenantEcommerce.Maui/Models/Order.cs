namespace MultiTenantEcommerce.Maui.Models;

public class Order
{
    public string Id { get; init; } = string.Empty;
    public DateTime OrderedAt { get; init; }
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string ShippingMethod { get; init; } = string.Empty;
    public Address ShippingAddress { get; init; } = new();
    public IReadOnlyList<OrderItem> Items { get; init; } = Array.Empty<OrderItem>();
}

public class OrderItem
{
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Variant { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}
