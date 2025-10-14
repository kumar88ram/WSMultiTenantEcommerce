using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using System.Collections.Generic;

namespace MultiTenantEcommerce.Maui.ViewModels;

[QueryProperty(nameof(OrderId), nameof(OrderId))]
public partial class RefundRequestViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _orderId = string.Empty;

    [ObservableProperty]
    private string _selectedReason = string.Empty;

    [ObservableProperty]
    private string _details = string.Empty;

    [ObservableProperty]
    private bool _isSubmitted;

    public IReadOnlyList<string> Reasons { get; } = new[]
    {
        "Item arrived damaged",
        "Wrong item delivered",
        "Quality issue",
        "Changed my mind"
    };

    public RefundRequestViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Request refund";
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedReason))
        {
            await Shell.Current.DisplayAlert("Refund", "Select a reason.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var request = new RefundRequest
            {
                OrderId = OrderId,
                Reason = SelectedReason,
                Details = Details
            };
            await _apiService.SubmitRefundRequestAsync(request);
            IsSubmitted = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Refund", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
