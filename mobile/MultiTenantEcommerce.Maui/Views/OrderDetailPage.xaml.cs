using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class OrderDetailPage : ContentPage
{
    public OrderDetailPage(OrderDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
