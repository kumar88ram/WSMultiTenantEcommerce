using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class CartViewModel : BaseViewModel
{
    private readonly CartService _cartService;

    public ObservableCollection<CartItem> Items => _cartService.Items;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxes;

    [ObservableProperty]
    private decimal _total;

    public CartViewModel(CartService cartService)
    {
        _cartService = cartService;
        Title = "Cart";
        _cartService.CartChanged += (_, _) => RefreshTotals();
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveItem(CartItem item)
    {
        _cartService.RemoveFromCart(item);
        RefreshTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        item.Quantity += 1;
        RefreshTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item.Quantity <= 1)
        {
            return;
        }

        item.Quantity -= 1;
        RefreshTotals();
    }

    [RelayCommand]
    private Task CheckoutAsync()
    {
        if (!Items.Any())
        {
            return Shell.Current.DisplayAlert("Cart empty", "Add items before checkout.", "OK");
        }

        return Shell.Current.GoToAsync(nameof(CheckoutPage));
    }

    public void RefreshTotals()
    {
        Subtotal = _cartService.Subtotal;
        Taxes = _cartService.Taxes;
        Total = _cartService.Total(0);
        IsEmpty = !Items.Any();
    }
}
