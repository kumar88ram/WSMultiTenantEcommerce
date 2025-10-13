using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AdminDbContext _dbContext;

    public AnalyticsService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SiteAnalyticsResponse> GetSiteSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalTenantsTask = _dbContext.Tenants.CountAsync(cancellationToken);
        var activeTenantsTask = _dbContext.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var activeStoresTask = _dbContext.Tenants.CountAsync(t => t.IsActive && t.CustomDomain != null, cancellationToken);
        var monthlyRevenueTask = _dbContext.Subscriptions
            .Where(s => s.Status == "Active")
            .SumAsync(s => (decimal?)s.Amount, cancellationToken);

        await Task.WhenAll(totalTenantsTask, activeTenantsTask, activeStoresTask, monthlyRevenueTask);

        var monthlyRevenue = monthlyRevenueTask.Result ?? 0m;
        var annualRevenue = monthlyRevenue * 12;

        return new SiteAnalyticsResponse(
            totalTenantsTask.Result,
            activeTenantsTask.Result,
            activeStoresTask.Result,
            monthlyRevenue,
            annualRevenue);
    }
}
