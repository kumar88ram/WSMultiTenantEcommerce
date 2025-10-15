using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.Theming;

public class ThemeCloneService : IThemeCloneService
{
    private readonly ApplicationDbContext _dbContext;

    public ThemeCloneService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> CloneTenantThemeAsync(Guid sourceTenantId, Guid targetTenantId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var sourceTenantTheme = await _dbContext.TenantThemes
            .Include(tt => tt.Variables)
            .Where(tt => tt.TenantId == sourceTenantId && tt.IsActive)
            .OrderByDescending(tt => tt.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceTenantTheme is null)
        {
            return null;
        }

        var newTenantTheme = new TenantTheme
        {
            TenantId = targetTenantId,
            ThemeId = sourceTenantTheme.ThemeId,
            ActivatedAt = DateTime.UtcNow,
            IsActive = false
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _dbContext.TenantThemes.Add(newTenantTheme);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (sourceTenantTheme.Variables.Any())
            {
                var clonedVariables = sourceTenantTheme.Variables.Select(v => new ThemeVariable
                {
                    TenantThemeId = newTenantTheme.Id,
                    Key = v.Key,
                    Value = v.Value
                });

                await _dbContext.ThemeVariables.AddRangeAsync(clonedVariables, cancellationToken);
            }

            var themeSections = await _dbContext.ThemeSections
                .Where(s => s.ThemeId == sourceTenantTheme.ThemeId)
                .ToListAsync(cancellationToken);

            if (themeSections.Any())
            {
                var clonedSections = themeSections.Select(section => new TenantThemeSection
                {
                    TenantThemeId = newTenantTheme.Id,
                    TenantId = targetTenantId,
                    SectionName = section.SectionName,
                    JsonConfig = section.JsonConfig,
                    SortOrder = section.SortOrder
                });

                await _dbContext.TenantThemeSections.AddRangeAsync(clonedSections, cancellationToken);
            }

            await _dbContext.ThemeAuditLogs.AddAsync(new ThemeAuditLog
            {
                ThemeId = sourceTenantTheme.ThemeId,
                AdminId = adminId,
                SourceTenantId = sourceTenantId,
                TargetTenantId = targetTenantId,
                Action = "Clone"
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return newTenantTheme.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
