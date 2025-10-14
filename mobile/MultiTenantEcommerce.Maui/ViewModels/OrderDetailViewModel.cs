using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

[QueryProperty(nameof(OrderId), nameof(OrderId))]
public partial class OrderDetailViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _orderId = string.Empty;

    [ObservableProperty]
    private Order? _order;

    [ObservableProperty]
    private ObservableCollection<OrderItem> _items = new();

    public OrderDetailViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Order Detail";
    }

    partial void OnOrderIdChanged(string value)
    {
        _ = LoadOrderAsync();
    }

    private async Task LoadOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderId))
        {
            return;
        }

        try
        {
            IsBusy = true;
            var order = await _apiService.GetOrderAsync(OrderId);
            if (order is null)
            {
                await Shell.Current.DisplayAlert("Orders", "Unable to load order details.", "OK");
                return;
            }

            Order = order;
            Items = new ObservableCollection<OrderItem>(order.Items);
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
}
