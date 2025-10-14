using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IAnalyticsService
{
    Task<SiteAnalyticsResponse> GetSiteSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalyticsVisitPoint>> GetVisitSeriesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalyticsSalesPoint>> GetSalesByDateAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversionRatePoint>> GetConversionRatesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnalyticsEventDto>> GetSampleEventsAsync(int take = 25, CancellationToken cancellationToken = default);
}
