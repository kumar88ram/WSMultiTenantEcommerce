using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AdminDbContext _adminDbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;

    public AnalyticsService(
        AdminDbContext adminDbContext,
        ITenantResolver tenantResolver,
        ITenantDbContextFactory tenantDbContextFactory)
    {
        _adminDbContext = adminDbContext;
        _tenantResolver = tenantResolver;
        _tenantDbContextFactory = tenantDbContextFactory;
    }

    public async Task<SiteAnalyticsResponse> GetSiteSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalTenantsTask = _adminDbContext.Tenants.CountAsync(cancellationToken);
        var activeTenantsTask = _adminDbContext.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var activeStoresTask = _adminDbContext.Tenants.CountAsync(t => t.IsActive && t.CustomDomain != null, cancellationToken);
        var monthlyRevenueTask = _adminDbContext.Subscriptions
            .Where(s => s.Status == "Active")
            .SumAsync(s => (decimal?)s.Amount, cancellationToken);

        await Task.WhenAll(totalTenantsTask, activeTenantsTask, activeStoresTask, monthlyRevenueTask).ConfigureAwait(false);

        var monthlyRevenue = monthlyRevenueTask.Result ?? 0m;
        var annualRevenue = monthlyRevenue * 12;

        return new SiteAnalyticsResponse(
            totalTenantsTask.Result,
            activeTenantsTask.Result,
            activeStoresTask.Result,
            monthlyRevenue,
            annualRevenue);
    }

    public async Task<IReadOnlyList<AnalyticsVisitPoint>> GetVisitSeriesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(from, to);

        await using var tenantDb = CreateTenantDbContext();
        var summaries = await LoadSummarySnapshotsAsync(tenantDb, start, end, cancellationToken).ConfigureAwait(false);

        var results = new List<AnalyticsVisitPoint>();
        foreach (var date in EnumerateDates(start, end))
        {
            var snapshot = summaries.TryGetValue(date, out var value) ? value : SummarySnapshot.Empty;
            results.Add(new AnalyticsVisitPoint(date, snapshot.VisitCount));
        }

        return results;
    }

    public async Task<IReadOnlyList<AnalyticsSalesPoint>> GetSalesByDateAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(from, to);

        await using var tenantDb = CreateTenantDbContext();
        var summaries = await LoadSummarySnapshotsAsync(tenantDb, start, end, cancellationToken).ConfigureAwait(false);

        var results = new List<AnalyticsSalesPoint>();
        foreach (var date in EnumerateDates(start, end))
        {
            var snapshot = summaries.TryGetValue(date, out var value) ? value : SummarySnapshot.Empty;
            results.Add(new AnalyticsSalesPoint(date, decimal.Round(value.SalesAmount, 2), value.OrderCount));
        }

        return results;
    }

    public async Task<IReadOnlyList<ConversionRatePoint>> GetConversionRatesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (start, end) = NormalizeRange(from, to);

        await using var tenantDb = CreateTenantDbContext();
        var summaries = await LoadSummarySnapshotsAsync(tenantDb, start, end, cancellationToken).ConfigureAwait(false);

        var results = new List<ConversionRatePoint>();
        foreach (var date in EnumerateDates(start, end))
        {
            var snapshot = summaries.TryGetValue(date, out var value) ? value : SummarySnapshot.Empty;
            results.Add(new ConversionRatePoint(date, snapshot.ConversionRate, snapshot.VisitCount, snapshot.OrderCount, decimal.Round(snapshot.SalesAmount, 2)));
        }

        return results;
    }

    public async Task<IReadOnlyList<AnalyticsEventDto>> GetSampleEventsAsync(int take = 25, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 10;
        }

        await using var tenantDb = CreateTenantDbContext();
        var events = await tenantDb.AnalyticsEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .Take(take)
            .Select(e => new AnalyticsEventDto(e.Id, e.EventType, e.OccurredAt, e.Amount, e.Metadata))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (events.Count > 0)
        {
            return events;
        }

        return GenerateSyntheticEvents(take);
    }

    private ApplicationDbContext CreateTenantDbContext()
    {
        if (_tenantResolver.CurrentTenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not available for analytics queries.");
        }

        if (string.IsNullOrWhiteSpace(_tenantResolver.ConnectionString))
        {
            throw new InvalidOperationException("Tenant connection string was not resolved for analytics queries.");
        }

        return _tenantDbContextFactory.CreateDbContext(
            _tenantResolver.ConnectionString!,
            _tenantResolver.CurrentTenantId,
            _tenantResolver.TenantIdentifier);
    }

    private static (DateOnly Start, DateOnly End) NormalizeRange(DateOnly from, DateOnly to)
    {
        return from <= to ? (from, to) : (to, from);
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static decimal CalculateConversionRate(int visits, int orders)
    {
        if (visits <= 0 || orders <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)orders / visits, 4, MidpointRounding.AwayFromZero);
    }

    private static async Task<Dictionary<DateOnly, SummarySnapshot>> LoadSummarySnapshotsAsync(
        ApplicationDbContext tenantDb,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken)
    {
        var summaries = await tenantDb.DailyAnalyticsSummaries
            .AsNoTracking()
            .Where(summary => summary.Date >= start && summary.Date <= end)
            .Select(summary => new
            {
                summary.Date,
                summary.VisitCount,
                summary.OrderCount,
                summary.SalesAmount,
                summary.ConversionRate
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lookup = summaries.ToDictionary(
            summary => summary.Date,
            summary => new SummarySnapshot(
                summary.VisitCount,
                summary.OrderCount,
                summary.SalesAmount,
                summary.ConversionRate));

        if (lookup.Count > 0)
        {
            return lookup;
        }

        var rangeStart = start.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = end.ToDateTime(TimeOnly.MinValue).AddDays(1);

        var fallback = await tenantDb.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.OccurredAt >= rangeStart && evt.OccurredAt < rangeEnd)
            .GroupBy(evt => evt.OccurredAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                Visits = group.Sum(evt => evt.EventType == AnalyticsEventType.Visit ? 1 : 0),
                Orders = group.Sum(evt => evt.EventType == AnalyticsEventType.OrderCompleted ? 1 : 0),
                Sales = group.Sum(evt => evt.EventType == AnalyticsEventType.OrderCompleted ? (evt.Amount ?? 0m) : 0m)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in fallback)
        {
            var date = DateOnly.FromDateTime(item.Date);
            lookup[date] = new SummarySnapshot(
                item.Visits,
                item.Orders,
                item.Sales,
                CalculateConversionRate(item.Visits, item.Orders));
        }

        return lookup;
    }

    private static IReadOnlyList<AnalyticsEventDto> GenerateSyntheticEvents(int take)
    {
        var now = DateTime.UtcNow;
        var list = new List<AnalyticsEventDto>(take);

        for (var index = 0; index < take; index++)
        {
            var occurredAt = now.AddMinutes(-15 * index);
            var eventType = index % 4 switch
            {
                0 => AnalyticsEventType.Visit,
                1 => AnalyticsEventType.ProductViewed,
                2 => AnalyticsEventType.CheckoutStarted,
                _ => AnalyticsEventType.OrderCompleted
            };

            decimal? amount = eventType == AnalyticsEventType.OrderCompleted
                ? decimal.Round(25 + (index % 5) * 12.5m, 2)
                : null;

            var metadata = eventType switch
            {
                AnalyticsEventType.Visit => "Synthetic visit event",
                AnalyticsEventType.ProductViewed => "Synthetic product view",
                AnalyticsEventType.CheckoutStarted => "Synthetic checkout start",
                AnalyticsEventType.OrderCompleted => "Synthetic order completion",
                _ => null
            };

            list.Add(new AnalyticsEventDto(Guid.NewGuid(), eventType, occurredAt, amount, metadata));
        }

        return list;
    }

    private readonly record struct SummarySnapshot(int VisitCount, int OrderCount, decimal SalesAmount, decimal ConversionRate)
    {
        public static SummarySnapshot Empty { get; } = new(0, 0, 0m, 0m);
    }
}
