using MultiTenantEcommerce.Maui.ViewModels;

namespace MultiTenantEcommerce.Maui.Views;

public partial class SupportTicketsPage : ContentPage
{
    private SupportTicketsViewModel ViewModel => (SupportTicketsViewModel)BindingContext;

    public SupportTicketsPage(SupportTicketsViewModel viewModel)
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
