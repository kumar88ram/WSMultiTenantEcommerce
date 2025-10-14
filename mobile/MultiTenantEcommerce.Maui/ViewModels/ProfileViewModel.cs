using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private readonly Func<OtpLoginPage> _loginPageFactory;

    [ObservableProperty]
    private UserProfile? _profile;

    [ObservableProperty]
    private ObservableCollection<OrderSummary> _recentOrders = new();

    [ObservableProperty]
    private ObservableCollection<SupportTicket> _supportTickets = new();

    public ProfileViewModel(ApiService apiService, AuthService authService, Func<OtpLoginPage> loginPageFactory)
    {
        _apiService = apiService;
        _authService = authService;
        _loginPageFactory = loginPageFactory;
        Title = "Profile";
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
            Profile = await _apiService.GetProfileAsync();
            var orders = await _apiService.GetOrderHistoryAsync();
            RecentOrders = new ObservableCollection<OrderSummary>(orders.Take(3));
            var tickets = await _apiService.GetSupportTicketsAsync();
            SupportTickets = new ObservableCollection<SupportTicket>(tickets);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Profile", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ViewAllOrdersAsync()
    {
        return Shell.Current.GoToAsync(nameof(OrderHistoryPage));
    }

    [RelayCommand]
    private Task ManageAddressesAsync()
    {
        return Shell.Current.GoToAsync(nameof(AddressBookPage));
    }

    [RelayCommand]
    private Task ViewSupportTicketsAsync()
    {
        return Shell.Current.GoToAsync(nameof(SupportTicketsPage));
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Shell.Current.DisplayAlert("Security", "Password change is managed on the web portal.", "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var loginPage = _loginPageFactory();
            Application.Current.MainPage = new NavigationPage(loginPage);
        });
    }
}
