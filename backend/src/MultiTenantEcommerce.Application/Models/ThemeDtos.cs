using System;
using System.Collections.Generic;
using System.Linq;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record ThemeSummaryDto(
    Guid Id,
    string Name,
    string Code,
    string Version,
    string? Description,
    string? PreviewImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    IEnumerable<ThemeSectionDto> Sections);

public record ThemeSectionDto(
    Guid Id,
    string SectionName,
    string JsonConfig,
    int SortOrder);

public record TenantThemeDto(
    Guid TenantThemeId,
    Guid TenantId,
    ThemeSummaryDto Theme,
    DateTime ActivatedAt,
    bool IsActive,
    IEnumerable<ThemeVariableDto> Variables);

public record ThemeVariableDto(string Key, string Value);

public record ThemeUsageSummaryDto(
    Guid ThemeId,
    string ThemeName,
    int ActiveTenantsCount,
    double AverageActiveDays,
    IEnumerable<TenantUsageSnapshot> TopTenants);

public record TenantUsageSnapshot(Guid TenantId, double TotalActiveDays, DateTime? ActivatedAt);

public record TenantThemeUsageDto(
    Guid TenantId,
    Guid ThemeId,
    bool IsActive,
    DateTime ActivatedAt,
    DateTime? DeactivatedAt,
    double TotalActiveDays);

public static class ThemeMappings
{
    public static ThemeSummaryDto ToSummaryDto(this Theme theme)
    {
        return new ThemeSummaryDto(
            theme.Id,
            theme.Name,
            theme.Code,
            theme.Version,
            theme.Description,
            theme.PreviewImageUrl,
            theme.IsActive,
            theme.CreatedAt,
            theme.Sections
                .OrderBy(s => s.SortOrder)
                .Select(s => new ThemeSectionDto(s.Id, s.SectionName, s.JsonConfig, s.SortOrder)));
    }

    public static TenantThemeDto ToTenantThemeDto(this TenantTheme tenantTheme)
    {
        return new TenantThemeDto(
            tenantTheme.Id,
            tenantTheme.TenantId,
            tenantTheme.Theme?.ToSummaryDto() ?? throw new InvalidOperationException("Tenant theme is missing theme metadata"),
            tenantTheme.ActivatedAt,
            tenantTheme.IsActive,
            tenantTheme.Variables.Select(v => new ThemeVariableDto(v.Key, v.Value)));
    }
}
