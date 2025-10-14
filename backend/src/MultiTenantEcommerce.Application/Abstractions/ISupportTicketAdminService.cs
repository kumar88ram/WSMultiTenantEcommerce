using MultiTenantEcommerce.Application.Models.Support;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ISupportTicketAdminService
{
    Task<SupportTicketListResult> GetAsync(Guid? tenantId, SupportTicketListQuery query, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> GetByIdAsync(Guid tenantId, Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> UpdateAsync(Guid tenantId, Guid ticketId, UpdateSupportTicketRequest request, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> AddMessageAsync(Guid tenantId, Guid ticketId, AddSupportTicketMessageRequest request, CancellationToken cancellationToken = default);
}
