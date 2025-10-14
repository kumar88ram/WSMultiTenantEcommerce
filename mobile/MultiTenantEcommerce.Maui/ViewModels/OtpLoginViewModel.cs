using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class OtpLoginViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly TokenStorageService _tokenStorage;
    private readonly Func<ProfilePage> _profilePageFactory;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _otpCode = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOtpRequested;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private Color _statusColor = Colors.Transparent;

    public OtpLoginViewModel(ApiService apiService, TokenStorageService tokenStorage, Func<ProfilePage> profilePageFactory)
    {
        _apiService = apiService;
        _tokenStorage = tokenStorage;
        _profilePageFactory = profilePageFactory;
    }

    [RelayCommand]
    private async Task RequestOtpAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            StatusColor = Colors.Red;
            StatusMessage = "Enter a valid phone number.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = null;

            await _apiService.RequestOtpAsync(PhoneNumber);

            IsOtpRequested = true;
            StatusColor = Colors.Green;
            StatusMessage = "Verification code sent.";
        }
        catch (Exception ex)
        {
            StatusColor = Colors.Red;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task VerifyOtpAsync()
    {
        if (IsBusy || !IsOtpRequested)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(OtpCode))
        {
            StatusColor = Colors.Red;
            StatusMessage = "Enter the verification code.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = null;

            var result = await _apiService.VerifyOtpAsync(PhoneNumber, OtpCode);
            if (result is null)
            {
                StatusColor = Colors.Red;
                StatusMessage = "Invalid verification code.";
                return;
            }

            await _tokenStorage.SetAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt, result.Tenant);

            StatusColor = Colors.Green;
            StatusMessage = "Signed in successfully.";

            var profilePage = _profilePageFactory();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current.MainPage = new NavigationPage(profilePage);
            });
        }
        catch (Exception ex)
        {
            StatusColor = Colors.Red;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
