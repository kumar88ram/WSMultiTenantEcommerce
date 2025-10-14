using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly Func<OrderHistoryPage> _orderHistoryPageFactory;

    private bool _hasLoaded;

    [ObservableProperty]
    private UserProfile? _profile;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public ProfileViewModel(ApiService apiService, Func<OrderHistoryPage> orderHistoryPageFactory)
    {
        _apiService = apiService;
        _orderHistoryPageFactory = orderHistoryPageFactory;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Profile = await _apiService.GetProfileAsync();
            _hasLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToOrdersAsync()
    {
        var page = _orderHistoryPageFactory();
        if (Application.Current.MainPage is NavigationPage navigationPage)
        {
            await MainThread.InvokeOnMainThreadAsync(() => navigationPage.Navigation.PushAsync(page));
        }
    }

    public bool ShouldLoad() => !_hasLoaded;
}
