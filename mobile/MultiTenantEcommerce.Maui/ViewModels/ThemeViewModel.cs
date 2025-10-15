using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class ThemeViewModel : BaseViewModel
{
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private TenantTheme? _tenantTheme;

    [ObservableProperty]
    private string _logoUrl = string.Empty;

    [ObservableProperty]
    private string _primaryColor = "#2563EB";

    [ObservableProperty]
    private bool _isDarkMode;

    public ThemeViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        Title = "Theme";
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var theme = await _themeService.RefreshAsync();
            UpdateState(theme);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Theme", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        if (IsDarkMode)
        {
            _themeService.ApplySampleLightMode();
            IsDarkMode = false;
        }
        else
        {
            _themeService.ApplySampleDarkMode();
            IsDarkMode = true;
        }
    }

    [RelayCommand]
    private void ReapplyTheme()
    {
        if (_themeService.CurrentTheme is not null)
        {
            _themeService.ApplyTheme(_themeService.CurrentTheme);
            UpdateState(_themeService.CurrentTheme);
        }
    }

    private void UpdateState(TenantTheme theme)
    {
        TenantTheme = theme;
        LogoUrl = theme.Variables.FirstOrDefault(v => v.Key.Equals("logoUrl", StringComparison.OrdinalIgnoreCase))?.Value
                  ?? string.Empty;
        PrimaryColor = theme.Variables.FirstOrDefault(v => v.Key.Equals("primaryColor", StringComparison.OrdinalIgnoreCase))?.Value
                       ?? "#2563EB";
        IsDarkMode = Application.Current?.UserAppTheme == AppTheme.Dark;
    }
}
