using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Storage;

namespace MultiTenantEcommerce.Maui.Services;

public class AuthService
{
    private readonly ApiService _apiService;
    private readonly TokenStorageService _tokenStorageService;
    private readonly Dictionary<string, string> _otpCache = new();
    private readonly object _syncRoot = new();

    private const string OnboardingKey = "mt_onboarding_completed";

    public AuthService(ApiService apiService, TokenStorageService tokenStorageService)
    {
        _apiService = apiService;
        _tokenStorageService = tokenStorageService;
    }

    public bool HasCompletedOnboarding => Preferences.Get(OnboardingKey, false);

    public void MarkOnboardingCompleted()
    {
        Preferences.Set(OnboardingKey, true);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _tokenStorageService.GetAsync();
        return token is { ExpiresAt: var expires } && expires > DateTime.UtcNow;
    }

    public Task RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required", nameof(phoneNumber));
        }

        if (!_apiService.UseMockData)
        {
            return _apiService.RequestOtpAsync(phoneNumber, cancellationToken);
        }

        var code = new Random().Next(100000, 999999).ToString();
        lock (_syncRoot)
        {
            _otpCache[phoneNumber] = code;
        }

        WeakReferenceMessenger.Default.Send(new OtpGeneratedMessage(phoneNumber, code));
        return Task.CompletedTask;
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        string? cachedCode;
        lock (_syncRoot)
        {
            _otpCache.TryGetValue(phoneNumber, out cachedCode);
        }

        if (!_apiService.UseMockData)
        {
            var result = await _apiService.VerifyOtpAsync(phoneNumber, otpCode, cancellationToken);
            if (result is null)
            {
                return false;
            }

            await _tokenStorageService.SetAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt, result.Tenant);
            return true;
        }

        if (cachedCode is null || !string.Equals(cachedCode, otpCode, StringComparison.Ordinal))
        {
            return false;
        }

        lock (_syncRoot)
        {
            _otpCache.Remove(phoneNumber);
        }

        var accessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expiresAt = DateTime.UtcNow.AddHours(12);
        await _tokenStorageService.SetAsync(accessToken, refreshToken, expiresAt, tenant: "tenant-001");
        return true;
    }

    public async Task LogoutAsync()
    {
        _tokenStorageService.Clear();
        await Task.CompletedTask;
    }
}

public record OtpGeneratedMessage(string PhoneNumber, string Code);
