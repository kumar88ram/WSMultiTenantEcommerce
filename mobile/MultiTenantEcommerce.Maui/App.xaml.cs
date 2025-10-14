using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui;

public partial class App : Application
{
    public App(OtpLoginPage loginPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(loginPage);
    }
}
