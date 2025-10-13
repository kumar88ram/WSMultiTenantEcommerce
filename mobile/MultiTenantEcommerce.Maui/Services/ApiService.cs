using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MultiTenantEcommerce.Maui.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;

    public ApiService(TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001")
        };
    }

    public async Task<HttpResponseMessage> PostAsync(string path, object payload, string? tenant = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        await AttachHeadersAsync(request, tenant);

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string path, string? tenant = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        await AttachHeadersAsync(request, tenant);
        return await _httpClient.SendAsync(request, cancellationToken);
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
}
