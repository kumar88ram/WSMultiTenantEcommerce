namespace MultiTenantEcommerce.Application.Models;

public record SiteAnalyticsResponse(
    int TotalTenants,
    int ActiveTenants,
    int ActiveStores,
    decimal MonthlyRecurringRevenue,
    decimal AnnualRecurringRevenue
);

public record AnalyticsVisitPoint(DateOnly Date, int VisitCount);

public record AnalyticsSalesPoint(DateOnly Date, decimal SalesAmount, int OrderCount);

public record ConversionRatePoint(DateOnly Date, decimal ConversionRate, int VisitCount, int OrderCount, decimal SalesAmount);

public record AnalyticsEventDto(Guid Id, string EventType, DateTime OccurredAt, decimal? Amount, string? Metadata);
