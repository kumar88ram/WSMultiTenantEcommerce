using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ITenantBackupService
{
    Task TriggerBackupAsync(Guid tenantId, string tenantIdentifier, CancellationToken cancellationToken = default);
}
