using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;
using System.Collections.Generic;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class OrderHistoryViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<OrderSummary> _orders = new();

    public OrderHistoryViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Orders";
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
            var orders = await _apiService.GetOrderHistoryAsync();
            Orders = new ObservableCollection<OrderSummary>(orders);
            IsEmpty = Orders.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Orders", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ViewOrderAsync(OrderSummary summary)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(OrderDetailViewModel.OrderId)] = summary.OrderId
        };
        return Shell.Current.GoToAsync(nameof(OrderDetailPage), parameters);
    }

    [RelayCommand]
    private Task RequestRefundAsync(OrderSummary summary)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(RefundRequestViewModel.OrderId)] = summary.OrderId
        };
        return Shell.Current.GoToAsync(nameof(RefundRequestPage), parameters);
    }
}
