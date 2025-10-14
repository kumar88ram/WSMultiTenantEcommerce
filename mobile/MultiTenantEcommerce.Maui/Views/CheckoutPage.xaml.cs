using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class CheckoutPage : ContentPage
{
    private CheckoutViewModel ViewModel => (CheckoutViewModel)BindingContext;

    public CheckoutPage(CheckoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.InitializeCommand.CanExecute(null))
        {
            ViewModel.InitializeCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.Dispose();
    }
}
