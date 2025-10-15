using System;
using System.Collections.Generic;

namespace MultiTenantEcommerce.Maui.Models;

public record ThemeVariable(string Key, string Value);

public record ThemeSection(string Id, string SectionName, string JsonConfig, int SortOrder);

public record ThemeSummary(
    string Id,
    string Name,
    string Code,
    string Version,
    string? Description,
    string? PreviewImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<ThemeSection> Sections);

public record TenantTheme(
    string TenantThemeId,
    string TenantId,
    ThemeSummary Theme,
    DateTime ActivatedAt,
    bool IsActive,
    IReadOnlyList<ThemeVariable> Variables);
