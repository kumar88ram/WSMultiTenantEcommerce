using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
