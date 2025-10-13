using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionResponse> CreateAsync(SubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionResponse?> UpdateAsync(Guid id, SubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
