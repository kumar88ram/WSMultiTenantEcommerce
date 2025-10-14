using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class CheckoutViewModel : BaseViewModel, IRecipient<PaymentResult>, IDisposable
{
    private readonly ApiService _apiService;
    private readonly CartService _cartService;

    [ObservableProperty]
    private ObservableCollection<Address> _addresses = new();

    [ObservableProperty]
    private ObservableCollection<ShippingOption> _shippingOptions = new();

    [ObservableProperty]
    private Address? _selectedAddress;

    [ObservableProperty]
    private ShippingOption? _selectedShippingOption;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxes;

    [ObservableProperty]
    private decimal _shippingFee;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private string? _paymentStatusMessage;

    [ObservableProperty]
    private bool _isProcessing;

    public CheckoutViewModel(ApiService apiService, CartService cartService)
    {
        _apiService = apiService;
        _cartService = cartService;
        Title = "Checkout";
        Subtotal = cartService.Subtotal;
        Taxes = cartService.Taxes;
        WeakReferenceMessenger.Default.Register(this);
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
            var addresses = await _apiService.GetAddressesAsync();
            Addresses = new ObservableCollection<Address>(addresses);
            SelectedAddress = Addresses.FirstOrDefault(a => a.IsDefault) ?? Addresses.FirstOrDefault();

            var shippingOptions = await _apiService.GetShippingOptionsAsync();
            ShippingOptions = new ObservableCollection<ShippingOption>(shippingOptions);
            SelectedShippingOption = ShippingOptions.FirstOrDefault();
            UpdateTotals();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Checkout", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaceOrderAsync()
    {
        if (IsProcessing)
        {
            return;
        }

        if (SelectedAddress is null || SelectedShippingOption is null)
        {
            await Shell.Current.DisplayAlert("Checkout", "Select shipping details before paying.", "OK");
            return;
        }

        try
        {
            IsProcessing = true;
            PaymentStatusMessage = "Redirecting to payment gateway...";
            await Task.Delay(1500);

            // In production invoke payment SDK and await deep link callback
            var orderId = $"ORD-{DateTime.UtcNow:HHmmss}";
            NotificationService.PublishPaymentResult("success", orderId);
        }
        catch (Exception ex)
        {
            PaymentStatusMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    partial void OnSelectedShippingOptionChanged(ShippingOption? value)
    {
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        Subtotal = _cartService.Subtotal;
        Taxes = _cartService.Taxes;
        ShippingFee = SelectedShippingOption?.Price ?? 0;
        GrandTotal = _cartService.Total(ShippingFee);
    }

    public void Receive(PaymentResult message)
    {
        PaymentStatusMessage = message.Status.Equals("success", StringComparison.OrdinalIgnoreCase)
            ? $"Payment successful for order {message.OrderId}."
            : $"Payment status: {message.Status}.";

        if (message.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            _cartService.Clear();
            Subtotal = 0;
            Taxes = 0;
            ShippingFee = 0;
            GrandTotal = 0;
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
