using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class ProductDetailPage : ContentPage
{
    public ProductDetailPage(ProductDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
