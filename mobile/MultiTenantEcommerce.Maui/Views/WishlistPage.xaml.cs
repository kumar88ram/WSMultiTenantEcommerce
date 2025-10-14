using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class WishlistPage : ContentPage
{
    private WishlistViewModel ViewModel => (WishlistViewModel)BindingContext;

    public WishlistPage(WishlistViewModel viewModel)
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
}
