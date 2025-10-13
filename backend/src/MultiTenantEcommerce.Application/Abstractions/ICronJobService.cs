using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ICronJobService
{
    Task<IEnumerable<CronJobResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<CronJobResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CronJobResponse> CreateAsync(CronJobRequest request, CancellationToken cancellationToken = default);
    Task<CronJobResponse?> UpdateAsync(Guid id, CronJobRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
