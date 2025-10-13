using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ISubscriptionPlanService
{
    Task<IEnumerable<SubscriptionPlanResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanResponse> CreateAsync(SubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanResponse?> UpdateAsync(Guid id, SubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
