using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class SplashViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly Func<AppShell> _shellFactory;
    private readonly Func<OnboardingPage> _onboardingPageFactory;
    private readonly Func<OtpLoginPage> _loginPageFactory;

    public SplashViewModel(AuthService authService, Func<AppShell> shellFactory, Func<OnboardingPage> onboardingPageFactory, Func<OtpLoginPage> loginPageFactory)
    {
        _authService = authService;
        _shellFactory = shellFactory;
        _onboardingPageFactory = onboardingPageFactory;
        _loginPageFactory = loginPageFactory;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await Task.Delay(1200);

            if (!_authService.HasCompletedOnboarding)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var onboarding = _onboardingPageFactory();
                    await Application.Current.MainPage.Navigation.PushAsync(onboarding);
                });
                return;
            }

            if (!await _authService.IsAuthenticatedAsync())
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var login = _loginPageFactory();
                    await Application.Current.MainPage.Navigation.PushAsync(login);
                });
                return;
            }

            await LaunchShellAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LaunchShellAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var shell = _shellFactory();
            Application.Current.MainPage = shell;
        });
    }
}
