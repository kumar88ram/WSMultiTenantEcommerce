using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.Theming;

public class ThemeAnalyticsService : IThemeAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;

    public ThemeAnalyticsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ThemeUsageSummaryDto>> GetThemeAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var usage = await _dbContext.ThemeUsageAnalytics
            .AsNoTracking()
            .GroupBy(x => x.ThemeId)
            .Select(group => new
            {
                ThemeId = group.Key,
                ActiveTenants = group.Count(x => x.IsActive),
                AverageDays = group.Select(x => x.TotalActiveDays).DefaultIfEmpty(0).Average(),
                TopTenants = group
                    .OrderByDescending(x => x.TotalActiveDays)
                    .Take(5)
                    .Select(x => new TenantUsageSnapshot(x.TenantId, x.TotalActiveDays, x.ActivatedAt))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var themeNames = await _dbContext.Themes
            .AsNoTracking()
            .Where(t => usage.Select(u => u.ThemeId).Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        return usage
            .Select(x => new ThemeUsageSummaryDto(
                x.ThemeId,
                themeNames.TryGetValue(x.ThemeId, out var name) ? name : string.Empty,
                x.ActiveTenants,
                Math.Round(x.AverageDays, 2),
                x.TopTenants))
            .ToList();
    }

    public async Task<IReadOnlyList<TenantThemeUsageDto>> GetThemeUsageAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        var usage = await _dbContext.ThemeUsageAnalytics
            .AsNoTracking()
            .Where(x => x.ThemeId == themeId)
            .OrderByDescending(x => x.ActivatedAt)
            .Select(x => new TenantThemeUsageDto(
                x.TenantId,
                x.ThemeId,
                x.IsActive,
                x.ActivatedAt,
                x.DeactivatedAt,
                x.TotalActiveDays))
            .ToListAsync(cancellationToken);

        return usage;
    }
}
