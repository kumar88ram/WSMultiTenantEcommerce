using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.ViewModels;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IConnectivity>(_ => Connectivity.Current);
        builder.Services.AddSingleton<TokenStorageService>();
        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://api.tenant-store.local")
            };
            return client;
        });

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<ThemeService>();

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<SplashPage>();

        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<OnboardingPage>();

        builder.Services.AddTransient<OtpLoginViewModel>();
        builder.Services.AddTransient<OtpLoginPage>();

        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();

        builder.Services.AddTransient<ProductListViewModel>();
        builder.Services.AddTransient<ProductListPage>();

        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<ProductDetailPage>();

        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<CartPage>();

        builder.Services.AddTransient<CheckoutViewModel>();
        builder.Services.AddTransient<CheckoutPage>();

        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<OrderHistoryPage>();

        builder.Services.AddTransient<OrderDetailViewModel>();
        builder.Services.AddTransient<OrderDetailPage>();

        builder.Services.AddTransient<RefundRequestViewModel>();
        builder.Services.AddTransient<RefundRequestPage>();

        builder.Services.AddTransient<WishlistViewModel>();
        builder.Services.AddTransient<WishlistPage>();

        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<ProfilePage>();

        builder.Services.AddTransient<AddressBookViewModel>();
        builder.Services.AddTransient<AddressBookPage>();

        builder.Services.AddTransient<SupportTicketsViewModel>();
        builder.Services.AddTransient<SupportTicketsPage>();
        builder.Services.AddTransient<ThemeViewModel>();

        return builder.Build();
    }
}
