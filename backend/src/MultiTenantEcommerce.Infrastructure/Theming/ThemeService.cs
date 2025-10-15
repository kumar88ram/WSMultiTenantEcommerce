using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.Theming;

public class ThemeService : IThemeService
{
    private readonly ApplicationDbContext _dbContext;
    public ThemeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Theme> UploadThemeAsync(ThemeUploadContext context, CancellationToken cancellationToken = default)
    {
        var storageDirectory = Path.Combine(context.StorageRoot, "themes");
        Directory.CreateDirectory(storageDirectory);

        var fileName = $"{context.Manifest.Code}-{context.Manifest.Version}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        var destinationPath = Path.Combine(storageDirectory, fileName);

        context.Content.Position = 0;
        await using (var fileStream = File.Create(destinationPath))
        {
            await context.Content.CopyToAsync(fileStream, cancellationToken);
        }

        var existing = await _dbContext.Themes
            .FirstOrDefaultAsync(t => t.Code == context.Manifest.Code && t.Version == context.Manifest.Version, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Theme {context.Manifest.Code} version {context.Manifest.Version} already exists.");
        }

        var entity = new Theme
        {
            Name = context.Manifest.Name,
            Code = context.Manifest.Code,
            Version = context.Manifest.Version,
            Description = context.Manifest.Description,
            PreviewImageUrl = context.Manifest.PreviewImageUrl,
            ZipFilePath = destinationPath,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Themes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<Theme>> GetThemesAsync(CancellationToken cancellationToken = default)
    {
        var themes = await _dbContext.Themes
            .AsNoTracking()
            .Include(t => t.Sections)
            .Include(t => t.TenantThemes)
            .ToListAsync(cancellationToken);

        foreach (var theme in themes)
        {
            theme.Sections = theme.Sections
                .OrderBy(s => s.SortOrder)
                .ToList();
        }

        return themes;
    }

    public async Task<TenantTheme?> ActivateThemeAsync(Guid themeId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var theme = await _dbContext.Themes.FirstOrDefaultAsync(t => t.Id == themeId, cancellationToken);
        if (theme is null)
        {
            return null;
        }

        var activeTheme = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(tt => tt.TenantId == tenantId && tt.IsActive, cancellationToken);
        if (activeTheme is not null)
        {
            activeTheme.IsActive = false;
            await FinalizeAnalyticsAsync(activeTheme.ThemeId, tenantId, DateTime.UtcNow, cancellationToken);
            _dbContext.TenantThemes.Update(activeTheme);
        }

        var tenantTheme = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(tt => tt.TenantId == tenantId && tt.ThemeId == themeId, cancellationToken);

        if (tenantTheme is null)
        {
            tenantTheme = new TenantTheme
            {
                TenantId = tenantId,
                ThemeId = themeId,
                ActivatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _dbContext.TenantThemes.Add(tenantTheme);
        }
        else
        {
            tenantTheme.IsActive = true;
            tenantTheme.ActivatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogActivationAsync(themeId, tenantId, tenantTheme.ActivatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return tenantTheme;
    }

    public async Task DeactivateThemeAsync(Guid themeId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantTheme = await _dbContext.TenantThemes
            .FirstOrDefaultAsync(tt => tt.TenantId == tenantId && tt.ThemeId == themeId, cancellationToken);

        if (tenantTheme is null)
        {
            return;
        }

        tenantTheme.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await FinalizeAnalyticsAsync(themeId, tenantId, DateTime.UtcNow, cancellationToken);
    }

    public async Task<TenantTheme?> GetActiveThemeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantThemes
            .AsNoTracking()
            .Include(tt => tt.Theme!)
            .Include(tt => tt.Variables)
            .Include(tt => tt.Sections)
            .Where(tt => tt.TenantId == tenantId && tt.IsActive)
            .OrderByDescending(tt => tt.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ThemeSection>> UpsertSectionsAsync(Guid themeId, IEnumerable<ThemeSectionDefinition> sections, CancellationToken cancellationToken = default)
    {
        var theme = await _dbContext.Themes.Include(t => t.Sections)
            .FirstOrDefaultAsync(t => t.Id == themeId, cancellationToken);
        if (theme is null)
        {
            throw new InvalidOperationException("Theme not found");
        }

        _dbContext.ThemeSections.RemoveRange(theme.Sections);

        var sectionEntities = sections.Select(section => new ThemeSection
        {
            ThemeId = themeId,
            SectionName = section.SectionName,
            JsonConfig = section.JsonConfig,
            SortOrder = section.SortOrder
        }).ToList();

        await _dbContext.ThemeSections.AddRangeAsync(sectionEntities, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return sectionEntities;
    }

    public async Task<IReadOnlyList<ThemeVariable>> UpdateVariablesAsync(Guid tenantThemeId, IDictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        var tenantTheme = await _dbContext.TenantThemes
            .Include(tt => tt.Variables)
            .FirstOrDefaultAsync(tt => tt.Id == tenantThemeId, cancellationToken);

        if (tenantTheme is null)
        {
            throw new InvalidOperationException("Tenant theme not found");
        }

        foreach (var kvp in variables)
        {
            var existing = tenantTheme.Variables.FirstOrDefault(v => v.Key == kvp.Key);
            if (existing is null)
            {
                tenantTheme.Variables.Add(new ThemeVariable
                {
                    TenantThemeId = tenantThemeId,
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
            else
            {
                existing.Value = kvp.Value;
            }
        }

        var keysToRemove = tenantTheme.Variables.Where(v => !variables.ContainsKey(v.Key)).ToList();
        foreach (var variable in keysToRemove)
        {
            tenantTheme.Variables.Remove(variable);
            _dbContext.ThemeVariables.Remove(variable);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return tenantTheme.Variables.ToList();
    }

    private async Task LogActivationAsync(Guid themeId, Guid tenantId, DateTime activatedAt, CancellationToken cancellationToken)
    {
        var analyticsEntry = new ThemeUsageAnalytics
        {
            ThemeId = themeId,
            TenantId = tenantId,
            ActivatedAt = activatedAt,
            IsActive = true,
            TotalActiveDays = 0
        };

        await _dbContext.ThemeUsageAnalytics.AddAsync(analyticsEntry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task FinalizeAnalyticsAsync(Guid themeId, Guid tenantId, DateTime deactivatedAt, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.ThemeUsageAnalytics
            .Where(x => x.ThemeId == themeId && x.TenantId == tenantId && x.IsActive)
            .OrderByDescending(x => x.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return;
        }

        entry.IsActive = false;
        entry.DeactivatedAt = deactivatedAt;
        var duration = (deactivatedAt - entry.ActivatedAt).TotalDays;
        if (duration > 0)
        {
            entry.TotalActiveDays += duration;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
