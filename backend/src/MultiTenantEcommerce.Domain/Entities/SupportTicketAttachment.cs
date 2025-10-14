namespace MultiTenantEcommerce.Domain.Entities;

public class SupportTicketAttachment : BaseEntity
{
    public Guid SupportTicketMessageId { get; set; }
    public SupportTicketMessage? SupportTicketMessage { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
