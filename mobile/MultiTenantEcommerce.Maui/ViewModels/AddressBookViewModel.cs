using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class AddressBookViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Address> _addresses = new();

    public AddressBookViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Addresses";
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            var addresses = await _apiService.GetAddressesAsync();
            Addresses = new ObservableCollection<Address>(addresses);
            IsEmpty = Addresses.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Addresses", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetDefault(Address address)
    {
        foreach (var entry in Addresses)
        {
            entry.IsDefault = entry.Id == address.Id;
        }
        Addresses = new ObservableCollection<Address>(Addresses);
    }

    [RelayCommand]
    private async Task AddAddressAsync()
    {
        await Shell.Current.DisplayAlert("Addresses", "Adding new addresses is handled on the web dashboard for this demo.", "OK");
    }
}
