using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public sealed class DailyAnalyticsAggregationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyAnalyticsAggregationWorker> _logger;
    private readonly IOptions<MultiTenancyOptions> _multiTenancyOptions;

    public DailyAnalyticsAggregationWorker(
        IServiceProvider serviceProvider,
        ILogger<DailyAnalyticsAggregationWorker> logger,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _multiTenancyOptions = multiTenancyOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AggregateAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while aggregating analytics data.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task AggregateAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var adminDbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var tenantDbContextFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();

        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            try
            {
                var connectionString = ResolveConnectionString(tenant);
                await using var tenantDb = tenantDbContextFactory.CreateDbContext(connectionString, tenant.Id, tenant.Subdomain);

                var earliestEventDate = await tenantDb.AnalyticsEvents
                    .AsNoTracking()
                    .OrderBy(evt => evt.OccurredAt)
                    .Select(evt => (DateTime?)evt.OccurredAt)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (earliestEventDate is null)
                {
                    continue;
                }

                var lastAggregatedDate = await tenantDb.DailyAnalyticsSummaries
                    .AsNoTracking()
                    .OrderByDescending(summary => summary.Date)
                    .Select(summary => (DateOnly?)summary.Date)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                var startDate = lastAggregatedDate is null
                    ? DateOnly.FromDateTime(earliestEventDate.Value.ToUniversalTime())
                    : lastAggregatedDate.Value.AddDays(1);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (startDate >= today)
                {
                    continue;
                }

                for (var date = startDate; date < today; date = date.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dayStart = date.ToDateTime(TimeOnly.MinValue);
                    var nextDayStart = dayStart.AddDays(1);

                    var visitsTask = tenantDb.AnalyticsEvents
                        .Where(evt => evt.EventType == AnalyticsEventType.Visit && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                        .CountAsync(cancellationToken);

                    var ordersTask = tenantDb.AnalyticsEvents
                        .Where(evt => evt.EventType == AnalyticsEventType.OrderCompleted && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                        .CountAsync(cancellationToken);

                    var salesTask = tenantDb.AnalyticsEvents
                        .Where(evt => evt.EventType == AnalyticsEventType.OrderCompleted && evt.OccurredAt >= dayStart && evt.OccurredAt < nextDayStart)
                        .SumAsync(evt => evt.Amount ?? 0m, cancellationToken);

                    await Task.WhenAll(visitsTask, ordersTask, salesTask).ConfigureAwait(false);

                    var visits = visitsTask.Result;
                    var orders = ordersTask.Result;
                    var sales = salesTask.Result;
                    var conversionRate = CalculateConversionRate(visits, orders);

                    var summary = await tenantDb.DailyAnalyticsSummaries
                        .FirstOrDefaultAsync(s => s.Date == date, cancellationToken)
                        .ConfigureAwait(false);

                    if (summary is null)
                    {
                        summary = new DailyAnalyticsSummary
                        {
                            TenantId = tenant.Id,
                            Date = date,
                            VisitCount = visits,
                            OrderCount = orders,
                            SalesAmount = sales,
                            ConversionRate = conversionRate,
                            CreatedAt = DateTime.UtcNow
                        };

                        tenantDb.DailyAnalyticsSummaries.Add(summary);
                    }
                    else
                    {
                        summary.VisitCount = visits;
                        summary.OrderCount = orders;
                        summary.SalesAmount = sales;
                        summary.ConversionRate = conversionRate;
                        summary.UpdatedAt = DateTime.UtcNow;
                    }

                    await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to aggregate analytics for tenant {TenantId} ({TenantName}).", tenant.Id, tenant.Name);
            }
        }
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
