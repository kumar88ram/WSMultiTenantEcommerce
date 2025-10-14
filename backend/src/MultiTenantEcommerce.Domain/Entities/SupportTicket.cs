using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class SupportTicket : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public ICollection<SupportTicketMessage> Messages { get; set; } = new Collection<SupportTicketMessage>();
}
