namespace MultiTenantEcommerce.Maui.Models;

public class SupportTicket
{
    public string Id { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; init; }
    public string LastMessagePreview { get; init; } = string.Empty;
}
