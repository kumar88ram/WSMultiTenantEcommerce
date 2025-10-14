using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class SupportTicketsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<SupportTicket> _tickets = new();

    public SupportTicketsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Support";
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            var tickets = await _apiService.GetSupportTicketsAsync();
            Tickets = new ObservableCollection<SupportTicket>(tickets);
            IsEmpty = Tickets.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Support", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewTicketAsync()
    {
        await Shell.Current.DisplayAlert("Support", "Submit new tickets from the web admin for this demo.", "OK");
    }
}
