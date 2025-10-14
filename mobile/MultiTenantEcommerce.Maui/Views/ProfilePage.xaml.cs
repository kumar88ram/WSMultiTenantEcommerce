using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class ProfilePage : ContentPage
{
    private ProfileViewModel ViewModel => (ProfileViewModel)BindingContext;

    public ProfilePage(ProfileViewModel viewModel)
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
