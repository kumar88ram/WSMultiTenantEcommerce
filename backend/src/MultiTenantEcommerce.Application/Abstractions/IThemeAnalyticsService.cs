using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IThemeAnalyticsService
{
    Task<IReadOnlyList<ThemeUsageSummaryDto>> GetThemeAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantThemeUsageDto>> GetThemeUsageAsync(Guid themeId, CancellationToken cancellationToken = default);
}
