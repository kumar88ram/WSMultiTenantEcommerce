using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class WishlistViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly CartService _cartService;

    [ObservableProperty]
    private ObservableCollection<Product> _items = new();

    public WishlistViewModel(ApiService apiService, CartService cartService)
    {
        _apiService = apiService;
        _cartService = cartService;
        Title = "Wishlist";
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
            var products = await _apiService.GetWishlistAsync();
            Items = new ObservableCollection<Product>(products);
            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Wishlist", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MoveToCartAsync(Product product)
    {
        _cartService.AddToCart(product, product.Variants.FirstOrDefault(), 1);
        await Shell.Current.DisplayAlert("Wishlist", $"{product.Name} moved to cart.", "OK");
    }
}
