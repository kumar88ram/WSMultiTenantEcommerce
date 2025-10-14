using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public sealed class LoggingTenantBackupService : ITenantBackupService
{
    private readonly ILogger<LoggingTenantBackupService> _logger;

    public LoggingTenantBackupService(ILogger<LoggingTenantBackupService> logger)
    {
        _logger = logger;
    }

    public Task TriggerBackupAsync(Guid tenantId, string tenantIdentifier, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Triggering logical backup for tenant {TenantId} ({TenantIdentifier}).",
            tenantId,
            tenantIdentifier);

        return Task.CompletedTask;
    }
}
