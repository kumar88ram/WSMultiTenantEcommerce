using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class RefundRequestPage : ContentPage
{
    public RefundRequestPage(RefundRequestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
