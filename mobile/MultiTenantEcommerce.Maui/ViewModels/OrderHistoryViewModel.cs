using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class OrderHistoryViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    private bool _hasLoaded;

    public ObservableCollection<OrderSummary> Orders { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public OrderHistoryViewModel(ApiService apiService)
    {
        _apiService = apiService;
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

            var orders = await _apiService.GetOrderHistoryAsync();

            Orders.Clear();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }

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

    public bool ShouldLoad() => !_hasLoaded;
}
