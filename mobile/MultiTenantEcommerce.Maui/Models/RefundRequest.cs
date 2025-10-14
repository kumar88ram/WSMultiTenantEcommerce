namespace MultiTenantEcommerce.Maui.Models;

public class RefundRequest
{
    public string OrderId { get; init; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}
