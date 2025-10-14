namespace MultiTenantEcommerce.Maui.Models;

public record OrderSummary(string OrderId, DateTime OrderDate, decimal Total, string Status, string PaymentStatus);
