using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IAdminTenantService
{
    Task<IEnumerable<TenantResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<TenantResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantResponse?> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
