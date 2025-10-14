using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class ProductListPage : ContentPage
{
    private ProductListViewModel ViewModel => (ProductListViewModel)BindingContext;

    public ProductListPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
