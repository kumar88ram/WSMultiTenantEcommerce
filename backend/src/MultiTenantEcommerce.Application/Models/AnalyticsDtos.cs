namespace MultiTenantEcommerce.Application.Models;

public record SiteAnalyticsResponse(
    int TotalTenants,
    int ActiveTenants,
    int ActiveStores,
    decimal MonthlyRecurringRevenue,
    decimal AnnualRecurringRevenue
);
