namespace MultiTenantEcommerce.Maui.Models;

public class NotificationMessage
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public bool IsRead { get; set; }
}
