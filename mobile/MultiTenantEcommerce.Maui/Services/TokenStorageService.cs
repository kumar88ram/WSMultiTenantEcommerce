using Microsoft.Maui.Storage;

namespace MultiTenantEcommerce.Maui.Services;

public class TokenStorageService
{
    private const string AccessTokenKey = "mt_access";
    private const string RefreshTokenKey = "mt_refresh";
    private const string ExpiresKey = "mt_expires";
    private const string TenantKey = "mt_tenant";

    public async Task SetAsync(string accessToken, string refreshToken, DateTime expiresAt, string tenant)
    {
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
        await SecureStorage.SetAsync(ExpiresKey, expiresAt.ToString("O"));
        await SecureStorage.SetAsync(TenantKey, tenant);
    }

    public async Task<TokenBundle?> GetAsync()
    {
        var accessToken = await SecureStorage.GetAsync(AccessTokenKey);
        var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
        var expiresRaw = await SecureStorage.GetAsync(ExpiresKey);
        var tenant = await SecureStorage.GetAsync(TenantKey);

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(expiresRaw) || string.IsNullOrEmpty(tenant))
        {
            return null;
        }

        return new TokenBundle(accessToken, refreshToken, DateTime.Parse(expiresRaw), tenant);
    }

    public void Clear()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        SecureStorage.Remove(ExpiresKey);
        SecureStorage.Remove(TenantKey);
    }
}

public record TokenBundle(string AccessToken, string RefreshToken, DateTime ExpiresAt, string Tenant);
