using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Models;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class OnboardingViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly Func<OtpLoginPage> _loginPageFactory;

    [ObservableProperty]
    private ObservableCollection<OnboardingSlide> _slides = new();

    [ObservableProperty]
    private int _currentIndex;

    public OnboardingViewModel(AuthService authService, Func<OtpLoginPage> loginPageFactory)
    {
        _authService = authService;
        _loginPageFactory = loginPageFactory;
        Title = "Welcome";
        Slides = new ObservableCollection<OnboardingSlide>(CreateSlides());
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        _authService.MarkOnboardingCompleted();
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var login = _loginPageFactory();
            await Application.Current.MainPage.Navigation.PushAsync(login);
        });
    }

    private static IEnumerable<OnboardingSlide> CreateSlides()
    {
        yield return new OnboardingSlide
        {
            Title = "Discover curated collections",
            Description = "Browse seasonal drops and tenant-exclusive campaigns.",
            Illustration = "onboarding1"
        };
        yield return new OnboardingSlide
        {
            Title = "Shop securely with OTP login",
            Description = "Sign in instantly using your verified mobile number.",
            Illustration = "onboarding2"
        };
        yield return new OnboardingSlide
        {
            Title = "Track orders end-to-end",
            Description = "Manage orders, refunds, and support tickets in one place.",
            Illustration = "onboarding3"
        };
    }
}
