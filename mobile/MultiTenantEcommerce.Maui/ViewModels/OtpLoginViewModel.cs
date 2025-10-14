using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class OtpLoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly SplashViewModel _splashViewModel;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private string _otpCode = string.Empty;

    [ObservableProperty]
    private bool _isOtpRequested;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private Color _statusColor = Colors.Transparent;

    [ObservableProperty]
    private int _secondsRemaining;

    private CancellationTokenSource? _countdownCts;

    public OtpLoginViewModel(AuthService authService, SplashViewModel splashViewModel)
    {
        _authService = authService;
        _splashViewModel = splashViewModel;
        Title = "Verify mobile";
    }

    [RelayCommand]
    private async Task RequestOtpAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = null;
            await _authService.RequestOtpAsync(PhoneNumber);

            IsOtpRequested = true;
            StatusMessage = "OTP sent to your number.";
            StatusColor = Colors.Green;
            StartCountdown();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            StatusColor = Colors.Red;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task VerifyOtpAsync()
    {
        if (!IsOtpRequested || string.IsNullOrWhiteSpace(OtpCode))
        {
            StatusColor = Colors.Red;
            StatusMessage = "Enter the 6-digit code.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Verifying...";
            StatusColor = Colors.Orange;
            var verified = await _authService.VerifyOtpAsync(PhoneNumber, OtpCode);
            if (!verified)
            {
                StatusMessage = "Invalid code. Try again.";
                StatusColor = Colors.Red;
                return;
            }

            await _splashViewModel.LaunchShellAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            StatusColor = Colors.Red;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartCountdown()
    {
        _countdownCts?.Cancel();
        _countdownCts = new CancellationTokenSource();

        SecondsRemaining = 60;
        _ = RunCountdownAsync(_countdownCts.Token);
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        while (SecondsRemaining > 0 && !token.IsCancellationRequested)
        {
            await Task.Delay(1000, token);
            SecondsRemaining--;
        }

        if (SecondsRemaining <= 0)
        {
            IsOtpRequested = false;
        }
    }
}
