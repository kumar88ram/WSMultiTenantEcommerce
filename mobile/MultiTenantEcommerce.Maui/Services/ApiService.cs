using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Networking;
using MultiTenantEcommerce.Maui.Models;

namespace MultiTenantEcommerce.Maui.Services;

public class ApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;
    private readonly IConnectivity _connectivity;

    private const int MaxRetries = 3;

    public ApiService(HttpClient httpClient, TokenStorageService tokenStorage, IConnectivity connectivity)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
        _connectivity = connectivity;
    }

    public Task<HttpResponseMessage> PostAsync(string path, object payload, string? tenant = null, CancellationToken cancellationToken = default)
    {
        return SendWithRetryAsync(() =>
        {
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return request;
        }, tenant, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(string path, string? tenant = null, CancellationToken cancellationToken = default)
    {
        return SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, path), tenant, cancellationToken);
    }

    public async Task RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsync("/api/auth/request-otp", new { phoneNumber }, cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to request verification code.");
    }

    public async Task<TokenBundle?> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        using var response = await PostAsync("/api/auth/verify-otp", new { phoneNumber, otpCode }, cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to verify code.");

        return await DeserializeAsync<TokenBundle>(response, cancellationToken);
    }

    public async Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync("/api/account/profile", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load profile.");

        return await DeserializeAsync<UserProfile>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrderHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync("/api/account/orders", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load order history.");

        var orders = await DeserializeAsync<List<OrderSummary>>(response, cancellationToken);
        return orders ?? Array.Empty<OrderSummary>();
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, string? tenant, CancellationToken cancellationToken)
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            throw new InvalidOperationException("No internet connection available.");
        }

        Exception? lastException = null;

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var request = requestFactory();
                await AttachHeadersAsync(request, tenant);
                var response = await _httpClient.SendAsync(request, cancellationToken);

                if ((int)response.StatusCode >= 500 && attempt < MaxRetries - 1)
                {
                    lastException = new HttpRequestException($"Server error: {(int)response.StatusCode}");
                }
                else
                {
                    return response;
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
        }

        throw lastException ?? new HttpRequestException("Request failed after retries.");
    }

    private async Task AttachHeadersAsync(HttpRequestMessage request, string? tenant)
    {
        if (!string.IsNullOrEmpty(tenant))
        {
            request.Headers.Add("X-Tenant", tenant);
        }

        var tokens = await _tokenStorage.GetAsync();
        if (tokens?.AccessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            if (string.IsNullOrEmpty(tenant))
            {
                request.Headers.Add("X-Tenant", tokens.Tenant);
            }
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string errorMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"{errorMessage} ({(int)response.StatusCode}). {details}");
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
    }
}
