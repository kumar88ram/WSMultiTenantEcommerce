using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;
using System.Collections.Generic;
using System.Linq;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Product> _featuredProducts = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<CampaignBanner> _campaigns = new();

    public HomeViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Title = "Nazmart";
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
            var feed = await _apiService.GetHomeFeedAsync();
            FeaturedProducts = new ObservableCollection<Product>(feed.FeaturedProducts);
            Categories = new ObservableCollection<Category>(feed.Categories);
            Campaigns = new ObservableCollection<CampaignBanner>(feed.Campaigns);
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

    [RelayCommand]
    private Task ViewProductAsync(Product product)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(ProductDetailViewModel.ProductId)] = product.Id
        };
        return Shell.Current.GoToAsync(nameof(ProductDetailPage), parameters);
    }

    [RelayCommand]
    private Task ViewCategoryAsync(Category category)
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(ProductListViewModel.CategoryId)] = category.Id,
            [nameof(ProductListViewModel.Title)] = category.Name
        };
        return Shell.Current.GoToAsync(nameof(ProductListPage), parameters);
    }

    [RelayCommand]
    private async Task ViewCampaignAsync(CampaignBanner campaign)
    {
        if (campaign.TargetAction.StartsWith("category:"))
        {
            var categoryId = campaign.TargetAction.Split(':')[1];
            var category = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category is not null)
            {
                await ViewCategoryAsync(category);
            }
        }
        else
        {
            await Shell.Current.DisplayAlert(campaign.Title, campaign.Subtitle, "Browse");
        }
    }
}
