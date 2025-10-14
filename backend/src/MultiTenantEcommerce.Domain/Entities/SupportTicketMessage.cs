using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class SupportTicketMessage : BaseEntity
{
    public Guid SupportTicketId { get; set; }
    public SupportTicket? SupportTicket { get; set; }
    public string Body { get; set; } = string.Empty;
    public SupportTicketActorType AuthorType { get; set; }
    public Guid? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }

    public ICollection<SupportTicketAttachment> Attachments { get; set; } = new Collection<SupportTicketAttachment>();
}
