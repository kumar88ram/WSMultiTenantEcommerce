
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public sealed class TenantBackgroundJobHostedService : BackgroundService
{
    private readonly ITenantBackgroundJobCoordinator _coordinator;
    private readonly ILogger<TenantBackgroundJobHostedService> _logger;

    public TenantBackgroundJobHostedService(
        ITenantBackgroundJobCoordinator coordinator,
        ILogger<TenantBackgroundJobHostedService> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var orderEmailTask = RunRecurringJobAsync(
            "order-email-queue",
            (token) => _coordinator.QueuePendingOrderEmailsAsync(token),
            TimeSpan.FromMinutes(5),
            stoppingToken,
            runImmediately: true);

        var reportTask = RunRecurringJobAsync(
            "daily-tenant-report",
            (token) => _coordinator.GenerateDailyTenantReportsAsync(token),
            TimeSpan.FromDays(1),
            stoppingToken,
            runImmediately: true);

        var couponCleanupTask = RunRecurringJobAsync(
            "expired-coupon-cleanup",
            (token) => _coordinator.CleanupExpiredCouponsAsync(token),
            TimeSpan.FromHours(1),
            stoppingToken,
            runImmediately: true);

        var backupTask = RunRecurringJobAsync(
            "tenant-db-backup",
            (token) => _coordinator.TriggerTenantDatabaseBackupsAsync(token),
            TimeSpan.FromDays(1),
            stoppingToken,
            runImmediately: false);

        return Task.WhenAll(orderEmailTask, reportTask, couponCleanupTask, backupTask);
    }

    private Task RunRecurringJobAsync(
        string jobName,
        Func<CancellationToken, Task> job,
        TimeSpan interval,
        CancellationToken stoppingToken,
        bool runImmediately)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (runImmediately)
                {
                    await InvokeJobAsync(jobName, job, stoppingToken).ConfigureAwait(false);
                }

                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    await InvokeJobAsync(jobName, job, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }, stoppingToken);
    }

    private async Task InvokeJobAsync(
        string jobName,
        Func<CancellationToken, Task> job,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing background job {JobName}.", jobName);
            await job(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Completed background job {JobName}.", jobName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job {JobName} failed.", jobName);
        }
    }
}
