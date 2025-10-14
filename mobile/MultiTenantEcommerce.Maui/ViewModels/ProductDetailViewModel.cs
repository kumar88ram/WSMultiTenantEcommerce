using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

[QueryProperty(nameof(ProductId), nameof(ProductId))]
public partial class ProductDetailViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly CartService _cartService;

    [ObservableProperty]
    private Product? _product;

    [ObservableProperty]
    private ObservableCollection<string> _imageGallery = new();

    [ObservableProperty]
    private ObservableCollection<ProductVariant> _variants = new();

    [ObservableProperty]
    private string? _selectedImage;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private string _productId = string.Empty;

    public ProductDetailViewModel(ApiService apiService, CartService cartService)
    {
        _apiService = apiService;
        _cartService = cartService;
    }

    partial void OnProductIdChanged(string value)
    {
        _ = LoadProductAsync();
    }

    [RelayCommand]
    private async Task AddToCartAsync()
    {
        if (Product is null)
        {
            return;
        }

        var variant = Variants.FirstOrDefault();
        _cartService.AddToCart(Product, variant, Quantity);
        await Shell.Current.DisplayAlert("Added", $"{Product.Name} added to cart.", "OK");
    }

    [RelayCommand]
    private void SelectImage(string imageUrl)
    {
        SelectedImage = imageUrl;
    }

    [RelayCommand]
    private void ChangeQuantity(int delta)
    {
        var newValue = Quantity + delta;
        Quantity = Math.Max(1, newValue);
    }

    private async Task LoadProductAsync()
    {
        try
        {
            IsBusy = true;
            var product = await _apiService.GetProductAsync(ProductId);
            if (product is null)
            {
                await Shell.Current.DisplayAlert("Not found", "The product is unavailable.", "OK");
                return;
            }

            Product = product;
            ImageGallery = new ObservableCollection<string>(product.ImageUrls);
            Variants = new ObservableCollection<ProductVariant>(product.Variants.Select(v => new ProductVariant
            {
                Name = v.Name,
                Options = v.Options,
                SelectedOption = string.IsNullOrEmpty(v.SelectedOption) ? v.Options.FirstOrDefault() ?? string.Empty : v.SelectedOption,
                PriceModifier = v.PriceModifier,
                Sku = v.Sku,
                AvailableStock = v.AvailableStock
            }));
            SelectedImage = ImageGallery.FirstOrDefault();
            Quantity = 1;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
