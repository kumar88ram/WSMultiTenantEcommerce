
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public sealed class TenantBackgroundJobCoordinator : ITenantBackgroundJobCoordinator
{
    private static readonly TimeSpan OrderEmailInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CouponCleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan DailyJobInterval = TimeSpan.FromDays(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantBackgroundJobCoordinator> _logger;
    private readonly IOptions<MultiTenancyOptions> _multiTenancyOptions;

    public TenantBackgroundJobCoordinator(
        IServiceProvider serviceProvider,
        ILogger<TenantBackgroundJobCoordinator> logger,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _multiTenancyOptions = multiTenancyOptions;
    }

    public async Task QueuePendingOrderEmailsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var adminDbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var tenantDbContextFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();
        var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailNotificationQueue>();

        var cronJob = await GetOrCreateCronJobAsync(adminDbContext, "order-email-queue", "*/5 * * * *", cancellationToken)
            .ConfigureAwait(false);

        var lastRun = cronJob.LastRunAt ?? DateTime.UtcNow.Subtract(OrderEmailInterval);
        var now = DateTime.UtcNow;

        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            try
            {
                var connectionString = ResolveConnectionString(tenant);
                await using var tenantDb = tenantDbContextFactory.CreateDbContext(connectionString, tenant.Id, tenant.Subdomain);

                var shipments = await tenantDb.Orders
                    .AsNoTracking()
                    .Where(order => order.ShippedAt.HasValue && order.ShippedAt > lastRun && order.ShippedAt <= now)
                    .Select(order => new
                    {
                        order.Email,
                        order.OrderNumber,
                        order.GrandTotal,
                        order.Currency,
                        order.CreatedAt,
                        order.ShippedAt
                    })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var shipment in shipments)
                {
                    await emailQueue.QueueAsync(new OrderEmailNotification(
                        OrderEmailNotificationType.OrderShipped,
                        tenant.Id,
                        shipment.Email,
                        shipment.OrderNumber,
                        shipment.GrandTotal,
                        shipment.Currency,
                        shipment.CreatedAt,
                        null,
                        shipment.GrandTotal,
                        "Automated resend triggered by background job"),
                        cancellationToken).ConfigureAwait(false);
                }

                if (shipments.Count > 0)
                {
                    _logger.LogInformation(
                        "Queued {Count} shipment notifications for tenant {TenantId} ({TenantName}).",
                        shipments.Count,
                        tenant.Id,
                        tenant.Name);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue shipment emails for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
            }
        }

        cronJob.LastRunAt = now;
        cronJob.NextRunAt = now.Add(OrderEmailInterval);
        await adminDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task GenerateDailyTenantReportsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var adminDbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var tenantDbContextFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();

        var cronJob = await GetOrCreateCronJobAsync(adminDbContext, "daily-tenant-report", "0 1 * * *", cancellationToken)
            .ConfigureAwait(false);

        var reportDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var dayStart = reportDate.ToDateTime(TimeOnly.MinValue);
        var nextDayStart = dayStart.AddDays(1);

        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            try
            {
                var connectionString = ResolveConnectionString(tenant);
                await using var tenantDb = tenantDbContextFactory.CreateDbContext(connectionString, tenant.Id, tenant.Subdomain);

                var visitsTask = tenantDb.AnalyticsEvents
                    .AsNoTracking()
                    .Where(evt => evt.EventType == AnalyticsEventType.Visit && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                    .CountAsync(cancellationToken);

                var ordersTask = tenantDb.AnalyticsEvents
                    .AsNoTracking()
                    .Where(evt => evt.EventType == AnalyticsEventType.OrderCompleted && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                    .CountAsync(cancellationToken);

                var salesTask = tenantDb.AnalyticsEvents
                    .AsNoTracking()
                    .Where(evt => evt.EventType == AnalyticsEventType.OrderCompleted && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                    .SumAsync(evt => evt.Amount ?? 0m, cancellationToken);

                await Task.WhenAll(visitsTask, ordersTask, salesTask).ConfigureAwait(false);

                var conversionRate = CalculateConversionRate(visitsTask.Result, ordersTask.Result);

                var summary = await tenantDb.DailyAnalyticsSummaries
                    .FirstOrDefaultAsync(s => s.Date == reportDate, cancellationToken)
                    .ConfigureAwait(false);

                if (summary is null)
                {
                    summary = new DailyAnalyticsSummary
                    {
                        TenantId = tenant.Id,
                        Date = reportDate,
                        VisitCount = visitsTask.Result,
                        OrderCount = ordersTask.Result,
                        SalesAmount = salesTask.Result,
                        ConversionRate = conversionRate,
                        CreatedAt = DateTime.UtcNow
                    };

                    tenantDb.DailyAnalyticsSummaries.Add(summary);
                }
                else
                {
                    summary.VisitCount = visitsTask.Result;
                    summary.OrderCount = ordersTask.Result;
                    summary.SalesAmount = salesTask.Result;
                    summary.ConversionRate = conversionRate;
                    summary.UpdatedAt = DateTime.UtcNow;
                }

                await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to aggregate daily report for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
            }
        }

        var completedAt = DateTime.UtcNow;
        cronJob.LastRunAt = completedAt;
        cronJob.NextRunAt = completedAt.Add(DailyJobInterval);
        await adminDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CleanupExpiredCouponsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var adminDbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var tenantDbContextFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();

        var cronJob = await GetOrCreateCronJobAsync(adminDbContext, "expired-coupon-cleanup", "0 * * * *", cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;

        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            try
            {
                var connectionString = ResolveConnectionString(tenant);
                await using var tenantDb = tenantDbContextFactory.CreateDbContext(connectionString, tenant.Id, tenant.Subdomain);

                var expiredCoupons = await tenantDb.Coupons
                    .Where(coupon => coupon.IsActive && coupon.ExpiresAt.HasValue && coupon.ExpiresAt < now)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (expiredCoupons.Count == 0)
                {
                    continue;
                }

                foreach (var coupon in expiredCoupons)
                {
                    coupon.IsActive = false;
                    coupon.UpdatedAt = now;
                }

                await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Marked {Count} expired coupons as inactive for tenant {TenantId} ({TenantName}).",
                    expiredCoupons.Count,
                    tenant.Id,
                    tenant.Name);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up expired coupons for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
            }
        }

        cronJob.LastRunAt = now;
        cronJob.NextRunAt = now.Add(CouponCleanupInterval);
        await adminDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task TriggerTenantDatabaseBackupsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var adminDbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var backupService = scope.ServiceProvider.GetService<ITenantBackupService>();

        var cronJob = await GetOrCreateCronJobAsync(adminDbContext, "tenant-db-backup", "30 2 * * *", cancellationToken)
            .ConfigureAwait(false);

        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (backupService is null)
        {
            foreach (var tenant in tenants)
            {
                _logger.LogInformation("Background job would trigger backup for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
            }
        }
        else
        {
            foreach (var tenant in tenants)
            {
                try
                {
                    await backupService.TriggerBackupAsync(tenant.Id, tenant.Subdomain ?? tenant.Name, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to trigger backup for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
                }
            }
        }

        var runTimestamp = DateTime.UtcNow;
        cronJob.LastRunAt = runTimestamp;
        cronJob.NextRunAt = runTimestamp.Add(DailyJobInterval);
        await adminDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<CronJob> GetOrCreateCronJobAsync(AdminDbContext adminDbContext, string name, string schedule, CancellationToken cancellationToken)
    {
        var cronJob = await adminDbContext.CronJobs
            .FirstOrDefaultAsync(job => job.Name == name, cancellationToken)
            .ConfigureAwait(false);

        if (cronJob is not null)
        {
            return cronJob;
        }

        cronJob = new CronJob
        {
            Name = name,
            ScheduleExpression = schedule,
            Handler = $"{nameof(TenantBackgroundJobCoordinator)}.{name}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await adminDbContext.CronJobs.AddAsync(cronJob, cancellationToken).ConfigureAwait(false);
        await adminDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return cronJob;
    }

    private string ResolveConnectionString(Tenant tenant)
    {
        var options = _multiTenancyOptions.Value;
        if (options.UseSharedDatabase)
        {
            if (!string.IsNullOrWhiteSpace(options.SharedDatabaseConnectionString))
            {
                return options.SharedDatabaseConnectionString;
            }

            if (!string.IsNullOrWhiteSpace(options.AdminConnectionString))
            {
                return options.AdminConnectionString;
            }

            throw new InvalidOperationException("Shared database connection string is not configured.");
        }

        return tenant.DbConnectionString;
    }

    private static decimal CalculateConversionRate(int visits, int orders)
    {
        if (visits <= 0 || orders <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)orders / visits, 4, MidpointRounding.AwayFromZero);
    }
}
