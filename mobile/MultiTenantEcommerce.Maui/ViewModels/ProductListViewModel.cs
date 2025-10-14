using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;
using System.Collections.Generic;

namespace MultiTenantEcommerce.Maui.ViewModels;

[QueryProperty(nameof(CategoryId), nameof(CategoryId))]
[QueryProperty(nameof(Title), nameof(Title))]
public partial class ProductListViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private string _categoryId = string.Empty;

    public ProductListViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    partial void OnCategoryIdChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private Task ViewProductAsync(Product product)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(ProductDetailViewModel.ProductId)] = product.Id
        };
        return Shell.Current.GoToAsync(nameof(ProductDetailPage), parameters);
    }

    private async Task LoadProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryId))
        {
            return;
        }

        try
        {
            IsBusy = true;
            var items = await _apiService.GetProductsByCategoryAsync(CategoryId);
            Products = new ObservableCollection<Product>(items);
            IsEmpty = Products.Count == 0;
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
