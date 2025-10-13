using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IAnalyticsService
{
    Task<SiteAnalyticsResponse> GetSiteSummaryAsync(CancellationToken cancellationToken = default);
}
