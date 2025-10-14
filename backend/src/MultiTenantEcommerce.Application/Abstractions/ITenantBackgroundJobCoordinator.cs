using System.Threading;
using System.Threading.Tasks;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ITenantBackgroundJobCoordinator
{
    Task QueuePendingOrderEmailsAsync(CancellationToken cancellationToken = default);
    Task GenerateDailyTenantReportsAsync(CancellationToken cancellationToken = default);
    Task CleanupExpiredCouponsAsync(CancellationToken cancellationToken = default);
    Task TriggerTenantDatabaseBackupsAsync(CancellationToken cancellationToken = default);
}
