using MultiTenantEcommerce.Application.Models.Support;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ISupportTicketService
{
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicketListResult> GetAsync(SupportTicketListQuery query, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> UpdateAsync(Guid ticketId, UpdateSupportTicketRequest request, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> AddMessageAsync(Guid ticketId, AddSupportTicketMessageRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateCustomerAccessAsync(CustomerTicketLookup lookup, CancellationToken cancellationToken = default);
}
