using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Services;

namespace MultiTenantEcommerce.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly TokenStorageService _tokenStorage;

    [ObservableProperty]
    private string _tenant = string.Empty;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(ApiService apiService, TokenStorageService tokenStorage)
    {
        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var response = await _apiService.PostAsync("/api/auth/login", new
            {
                userName = UserName,
                password = Password
            }, Tenant);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Unable to sign in";
                return;
            }

            var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (payload is null)
            {
                ErrorMessage = "Invalid response";
                return;
            }

            await _tokenStorage.SetAsync(payload.AccessToken, payload.RefreshToken, payload.ExpiresAt, Tenant);

            await Application.Current.MainPage.DisplayAlert("Success", "Signed in successfully", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
