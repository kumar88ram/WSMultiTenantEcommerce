using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MultiTenantEcommerce.Maui.Models;

namespace MultiTenantEcommerce.Maui.Services;

public class ThemeService
{
    private readonly ApiService _apiService;

    public TenantTheme? CurrentTheme { get; private set; }

    public ThemeService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<TenantTheme> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var tenantTheme = await _apiService.GetTenantThemeAsync(cancellationToken);
        ApplyTheme(tenantTheme);
        return tenantTheme;
    }

    public void ApplyTheme(TenantTheme theme)
    {
        CurrentTheme = theme;
        var resources = Application.Current?.Resources ?? throw new InvalidOperationException("Application resources unavailable");

        var lookup = theme.Variables.ToDictionary(v => v.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        UpdateColor(resources, "PrimaryColor", lookup.TryGetValue("primaryColor", out var primary) ? primary : "#2563EB", "#2563EB");
        UpdateColor(resources, "SecondaryColor", lookup.TryGetValue("secondaryColor", out var secondary) ? secondary : "#7C3AED", "#7C3AED");
        UpdateColor(resources, "AccentColor", lookup.TryGetValue("accentColor", out var accent) ? accent : "#F97316", "#F97316");
        UpdateColor(resources, "BackgroundColor", lookup.TryGetValue("backgroundColor", out var background) ? background : "#F5F7FB", "#F5F7FB");
        UpdateColor(resources, "TextPrimaryColor", lookup.TryGetValue("textPrimaryColor", out var textPrimary) ? textPrimary : "#111827", "#111827");
        UpdateColor(resources, "TextSecondaryColor", lookup.TryGetValue("textSecondaryColor", out var textSecondary) ? textSecondary : "#6B7280", "#6B7280");

        resources["FontFamily"] = lookup.TryGetValue("fontFamily", out var fontFamily) ? fontFamily : "OpenSansRegular";
        resources["LogoUrl"] = lookup.TryGetValue("logoUrl", out var logoUrl) ? logoUrl : string.Empty;
    }

    public void ApplySampleDarkMode()
    {
        var resources = Application.Current?.Resources ?? throw new InvalidOperationException("Application resources unavailable");
        resources["PrimaryColor"] = Color.FromArgb("#111827");
        resources["SecondaryColor"] = Color.FromArgb("#1F2937");
        resources["AccentColor"] = Color.FromArgb("#F97316");
        resources["BackgroundColor"] = Color.FromArgb("#050816");
        resources["TextPrimaryColor"] = Color.FromArgb("#F9FAFB");
        resources["TextSecondaryColor"] = Color.FromArgb("#9CA3AF");
        resources["FontFamily"] = "Inter, sans-serif";
        Application.Current!.UserAppTheme = AppTheme.Dark;
    }

    public void ApplySampleLightMode()
    {
        var resources = Application.Current?.Resources ?? throw new InvalidOperationException("Application resources unavailable");
        resources["PrimaryColor"] = Color.FromArgb("#2563EB");
        resources["SecondaryColor"] = Color.FromArgb("#7C3AED");
        resources["AccentColor"] = Color.FromArgb("#F97316");
        resources["BackgroundColor"] = Color.FromArgb("#F5F7FB");
        resources["TextPrimaryColor"] = Color.FromArgb("#111827");
        resources["TextSecondaryColor"] = Color.FromArgb("#6B7280");
        resources["FontFamily"] = "OpenSansRegular";
        Application.Current!.UserAppTheme = AppTheme.Light;
    }

    private static void UpdateColor(ResourceDictionary resources, string key, string value, string fallback)
    {
        try
        {
            resources[key] = Color.FromArgb(value);
        }
        catch
        {
            resources[key] = Color.FromArgb(fallback);
        }
    }
}
