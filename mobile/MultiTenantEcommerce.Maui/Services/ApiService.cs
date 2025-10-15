using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Networking;
using MultiTenantEcommerce.Maui.Models;
using System.Linq;

namespace MultiTenantEcommerce.Maui.Services;

public class ApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly TokenStorageService _tokenStorage;
    private readonly IConnectivity _connectivity;

    public bool UseMockData { get; set; } = true;

    public ApiService(HttpClient httpClient, TokenStorageService tokenStorage, IConnectivity connectivity)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
        _connectivity = connectivity;
    }

    public async Task<HomeFeed> GetHomeFeedAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(300, cancellationToken);
            return MockData.HomeFeed;
        }

        using var response = await GetAsync("/api/storefront/home", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load home feed");
        return await DeserializeAsync<HomeFeed>(response, cancellationToken) ?? MockData.HomeFeed;
    }

    public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(300, cancellationToken);
            return MockData.Products.Where(p => p.CategoryId == categoryId).ToList();
        }

        using var response = await GetAsync($"/api/catalog/categories/{categoryId}/products", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load products");
        var products = await DeserializeAsync<List<Product>>(response, cancellationToken);
        return products ?? Array.Empty<Product>();
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Products.FirstOrDefault(p => p.Id == productId);
        }

        using var response = await GetAsync($"/api/catalog/products/{productId}", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load product");
        return await DeserializeAsync<Product>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetWishlistAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Products.Where(p => p.IsWishlisted).ToList();
        }

        using var response = await GetAsync("/api/account/wishlist", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load wishlist");
        var items = await DeserializeAsync<List<Product>>(response, cancellationToken);
        return items ?? Array.Empty<Product>();
    }

    public async Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Profile;
        }

        using var response = await GetAsync("/api/account/profile", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load profile");
        return await DeserializeAsync<UserProfile>(response, cancellationToken) ?? MockData.Profile;
    }

    public async Task<IReadOnlyList<Address>> GetAddressesAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(150, cancellationToken);
            return MockData.Profile.Addresses.ToList();
        }

        using var response = await GetAsync("/api/account/addresses", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load addresses");
        var addresses = await DeserializeAsync<List<Address>>(response, cancellationToken);
        return addresses ?? Array.Empty<Address>();
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrderHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Orders.Select(o => new OrderSummary(o.Id, o.OrderedAt, o.Total, o.Status, o.PaymentStatus)).ToList();
        }

        using var response = await GetAsync("/api/account/orders", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load orders");
        var orders = await DeserializeAsync<List<OrderSummary>>(response, cancellationToken);
        return orders ?? Array.Empty<OrderSummary>();
    }

    public async Task<Order?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.Orders.FirstOrDefault(o => o.Id == orderId);
        }

        using var response = await GetAsync($"/api/account/orders/{orderId}", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load order");
        return await DeserializeAsync<Order>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetSupportTicketsAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(200, cancellationToken);
            return MockData.SupportTickets;
        }

        using var response = await GetAsync("/api/support/tickets", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load support tickets");
        var tickets = await DeserializeAsync<List<SupportTicket>>(response, cancellationToken);
        return tickets ?? Array.Empty<SupportTicket>();
    }

    public async Task<IReadOnlyList<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(150, cancellationToken);
            return MockData.Notifications;
        }

        using var response = await GetAsync("/api/account/notifications", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load notifications");
        var notifications = await DeserializeAsync<List<NotificationMessage>>(response, cancellationToken);
        return notifications ?? Array.Empty<NotificationMessage>();
    }

    public async Task<TenantTheme> GetTenantThemeAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(150, cancellationToken);
            return MockData.ActiveTenantTheme;
        }

        using var response = await GetAsync("/tenant/theme", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load tenant theme");
        return await DeserializeAsync<TenantTheme>(response, cancellationToken)
               ?? MockData.ActiveTenantTheme;
    }

    public async Task<IReadOnlyList<ShippingOption>> GetShippingOptionsAsync(CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(150, cancellationToken);
            return MockData.ShippingOptions;
        }

        using var response = await GetAsync("/api/checkout/shipping-options", cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to load shipping options");
        var options = await DeserializeAsync<List<ShippingOption>>(response, cancellationToken);
        return options ?? Array.Empty<ShippingOption>();
    }

    public async Task SubmitRefundRequestAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        if (UseMockData)
        {
            await Task.Delay(300, cancellationToken);
            WeakReferenceMessenger.Default.Send(request);
            return;
        }

        using var response = await PostAsync("/api/account/refunds", request, cancellationToken: cancellationToken);
        await EnsureSuccessAsync(response, "Unable to submit refund request");
    }

    private async Task<HttpResponseMessage> PostAsync(string path, object payload, string? tenant = null, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(() =>
        {
            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return request;
        }, tenant, cancellationToken);
    }

    private Task<HttpResponseMessage> GetAsync(string path, string? tenant = null, CancellationToken cancellationToken = default)
    {
        return SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, path), tenant, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, string? tenant, CancellationToken cancellationToken)
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            throw new InvalidOperationException("No internet connection available.");
        }

        Exception? lastException = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var request = requestFactory();
                await AttachHeadersAsync(request, tenant);
                var response = await _httpClient.SendAsync(request, cancellationToken);

                if ((int)response.StatusCode >= 500 && attempt < 2)
                {
                    lastException = new HttpRequestException($"Server error: {(int)response.StatusCode}");
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)), cancellationToken);
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)), cancellationToken);
            }
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

    private static class MockData
    {
        public static readonly IReadOnlyList<Category> Categories = new List<Category>
        {
            new() { Id = "fashion", Name = "Fashion", Icon = "tshirt", Description = "Curated looks for every day." },
            new() { Id = "beauty", Name = "Beauty", Icon = "sparkles", Description = "Skincare & cosmetics" },
            new() { Id = "grocery", Name = "Grocery", Icon = "basket", Description = "Fresh and organic" },
            new() { Id = "electronics", Name = "Electronics", Icon = "devices", Description = "Smart devices" }
        };

        public static readonly IReadOnlyList<Product> Products = new List<Product>
        {
            new()
            {
                Id = "sku-hoodie",
                Name = "Nazmart Essential Hoodie",
                Description = "Soft cotton hoodie with minimalist logo, perfect for layering any season.",
                Price = 49.90m,
                CompareAtPrice = 59.90m,
                Rating = 4.7,
                RatingCount = 231,
                CategoryId = "fashion",
                ImageUrls = new []
                {
                    "https://images.unsplash.com/photo-1523381210434-271e8be1f52b",
                    "https://images.unsplash.com/photo-1542291026-7eec264c27ff"
                },
                Variants = new []
                {
                    new ProductVariant { Name = "Size", Options = new [] { "S", "M", "L", "XL" }, SelectedOption = "M", AvailableStock = 12 },
                    new ProductVariant { Name = "Color", Options = new [] { "Black", "Sand", "Navy" }, SelectedOption = "Black" }
                },
                IsFeatured = true,
                IsWishlisted = true
            },
            new()
            {
                Id = "sku-sneaker",
                Name = "AirFlex Knit Sneaker",
                Description = "Lightweight knit sneakers engineered for all day comfort with responsive foam soles.",
                Price = 89.00m,
                Rating = 4.5,
                RatingCount = 128,
                CategoryId = "fashion",
                ImageUrls = new []
                {
                    "https://images.unsplash.com/photo-1542291026-7eec264c27ff",
                    "https://images.unsplash.com/photo-1511550299371-9590d0f5f22c"
                },
                Variants = new []
                {
                    new ProductVariant { Name = "Size", Options = new [] { "7", "8", "9", "10" }, SelectedOption = "9", AvailableStock = 5 },
                    new ProductVariant { Name = "Color", Options = new [] { "White", "Grey" }, SelectedOption = "White" }
                },
                IsFeatured = true
            },
            new()
            {
                Id = "sku-serum",
                Name = "Radiance Vitamin C Serum",
                Description = "Daily antioxidant serum brightening complexion with stabilized Vitamin C.",
                Price = 32.50m,
                Rating = 4.9,
                RatingCount = 412,
                CategoryId = "beauty",
                ImageUrls = new [] { "https://images.unsplash.com/photo-1585386959984-a4155227cdea" },
                Variants = new []
                {
                    new ProductVariant { Name = "Size", Options = new [] { "30ml", "50ml" }, SelectedOption = "30ml", AvailableStock = 30 }
                }
            },
            new()
            {
                Id = "sku-blender",
                Name = "VitaMix Pro Blender",
                Description = "High-performance blender with smart presets for smoothies and soups.",
                Price = 229.00m,
                Rating = 4.6,
                RatingCount = 92,
                CategoryId = "electronics",
                ImageUrls = new [] { "https://images.unsplash.com/photo-1488900128323-21503983a07e" },
                Variants = Array.Empty<ProductVariant>()
            }
        };

        public static readonly IReadOnlyList<CampaignBanner> Campaigns = new List<CampaignBanner>
        {
            new() { Id = "spring", Title = "Spring Refresh", Subtitle = "Up to 40% off beauty essentials", ImageUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330", TargetAction = "category:beauty" },
            new() { Id = "free-shipping", Title = "Free Shipping Weekend", Subtitle = "No minimum spend", ImageUrl = "https://images.unsplash.com/photo-1526045478516-99145907023c", TargetAction = "promo:shipping" }
        };

        public static readonly HomeFeed HomeFeed = new()
        {
            FeaturedProducts = Products.Where(p => p.IsFeatured).ToList(),
            Categories = Categories,
            Campaigns = Campaigns
        };

        public static readonly IReadOnlyList<Order> Orders = new List<Order>
        {
            new()
            {
                Id = "ORD-1001",
                OrderedAt = DateTime.UtcNow.AddDays(-4),
                Total = 178.90m,
                Status = "Shipped",
                PaymentStatus = "Paid",
                ShippingMethod = "Express",
                ShippingAddress = Profile.Addresses.First(),
                Items = new []
                {
                    new OrderItem { ProductId = "sku-hoodie", Name = "Nazmart Essential Hoodie", Variant = "Black / M", Quantity = 1, UnitPrice = 49.90m },
                    new OrderItem { ProductId = "sku-sneaker", Name = "AirFlex Knit Sneaker", Variant = "White / 9", Quantity = 1, UnitPrice = 89.00m }
                }
            },
            new()
            {
                Id = "ORD-1000",
                OrderedAt = DateTime.UtcNow.AddDays(-12),
                Total = 65.00m,
                Status = "Delivered",
                PaymentStatus = "Paid",
                ShippingMethod = "Standard",
                ShippingAddress = Profile.Addresses.First(),
                Items = new []
                {
                    new OrderItem { ProductId = "sku-serum", Name = "Radiance Vitamin C Serum", Variant = "50ml", Quantity = 2, UnitPrice = 32.50m }
                }
            }
        };

        public static readonly UserProfile Profile = new()
        {
            FullName = "Jasmine Patel",
            Email = "jasmine.patel@example.com",
            PhoneNumber = "+15551234567",
            LoyaltyLevel = "Gold",
            LoyaltyPoints = 2150,
            Addresses = new []
            {
                new Address
                {
                    Id = "addr-home",
                    Label = "Home",
                    Recipient = "Jasmine Patel",
                    Line1 = "123 Market Street",
                    City = "Seattle",
                    State = "WA",
                    PostalCode = "98109",
                    Country = "USA",
                    PhoneNumber = "+15551234567",
                    IsDefault = true
                },
                new Address
                {
                    Id = "addr-office",
                    Label = "Office",
                    Recipient = "Jasmine Patel",
                    Line1 = "456 Innovation Way",
                    City = "Seattle",
                    State = "WA",
                    PostalCode = "98104",
                    Country = "USA",
                    PhoneNumber = "+15559876543"
                }
            }
        };

        public static readonly IReadOnlyList<SupportTicket> SupportTickets = new List<SupportTicket>
        {
            new() { Id = "TCK-501", Subject = "Need invoice copy", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-2), LastMessagePreview = "We have escalated your request" },
            new() { Id = "TCK-489", Subject = "Delayed shipment", Status = "Resolved", CreatedAt = DateTime.UtcNow.AddDays(-7), LastMessagePreview = "Package delivered" }
        };

        public static readonly IReadOnlyList<NotificationMessage> Notifications = new List<NotificationMessage>
        {
            new() { Id = "NTF-1", Title = "Flash sale starts now", Body = "Grab your favourites with 30% off", ReceivedAt = DateTime.UtcNow.AddHours(-5) },
            new() { Id = "NTF-2", Title = "Order ORD-1001 shipped", Body = "Track your package arriving tomorrow", ReceivedAt = DateTime.UtcNow.AddHours(-12) }
        };

        public static readonly IReadOnlyList<ShippingOption> ShippingOptions = new List<ShippingOption>
        {
            new() { Id = "standard", Name = "Standard Shipping", Price = 0m, EstimatedDuration = TimeSpan.FromDays(5) },
            new() { Id = "express", Name = "Express Shipping", Price = 12.50m, EstimatedDuration = TimeSpan.FromDays(2) }
        };

        public static readonly TenantTheme ActiveTenantTheme = new(
            TenantThemeId: "tenant-theme-001",
            TenantId: "tenant-001",
            Theme: new ThemeSummary(
                Id: "classic-storefront",
                Name: "Classic Storefront",
                Code: "classic",
                Version: "1.0.0",
                Description: "A clean, typography-first storefront template.",
                PreviewImageUrl: "https://images.unsplash.com/photo-1523275335684-37898b6baf30",
                IsActive: true,
                CreatedAt: DateTime.UtcNow.AddMonths(-2),
                Sections: new List<ThemeSection>
                {
                    new("hero", "Hero Banner", "{}", 0),
                    new("featured", "Featured Products", "{}", 1),
                    new("testimonials", "Testimonials", "{}", 2)
                }),
            ActivatedAt: DateTime.UtcNow.AddDays(-14),
            IsActive: true,
            Variables: new List<ThemeVariable>
            {
                new("primaryColor", "#2563EB"),
                new("accentColor", "#F97316"),
                new("fontFamily", "Inter, sans-serif"),
                new("logoUrl", "https://images.unsplash.com/photo-1521572267360-ee0c2909d518")
            });
    }
}
