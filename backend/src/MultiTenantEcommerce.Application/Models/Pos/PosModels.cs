namespace MultiTenantEcommerce.Application.Models.Pos;

public class PosSaleRequest
{
    public string Currency { get; set; } = "USD";
    public decimal TaxTotal { get; set; }
    public decimal? DiscountTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? PaymentReference { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? CustomerEmail { get; set; }
    public List<PosSaleItemRequest> Items { get; set; } = new();
}

public class PosSaleItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? OverrideName { get; set; }
    public string? OverrideSku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountAmount { get; set; }
}

public record PosSaleResponse(
    Guid OrderId,
    string OrderNumber,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string Currency,
    string PaymentMethod,
    string? PaymentReference);

public record InventorySyncRequest(
    Guid ProductId,
    Guid? ProductVariantId,
    int QuantityOnHand,
    DateTime? SyncedAtUtc);

public record InventorySyncResult(
    Guid ProductId,
    Guid? ProductVariantId,
    bool Succeeded,
    string? ErrorMessage);

public record ReceiptResponse(
    Guid OrderId,
    string OrderNumber,
    string ContentType,
    string Content);
