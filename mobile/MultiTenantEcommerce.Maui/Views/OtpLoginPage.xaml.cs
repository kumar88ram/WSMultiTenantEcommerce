using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class OtpLoginPage : ContentPage
{
    public OtpLoginPage(OtpLoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
