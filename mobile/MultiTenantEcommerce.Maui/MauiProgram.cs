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
                BaseAddress = new Uri("https://localhost:5001")
            };
            return client;
        });
        builder.Services.AddSingleton<ApiService>();

        builder.Services.AddTransient<OtpLoginViewModel>();
        builder.Services.AddTransient<OtpLoginPage>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<OrderHistoryPage>();

        return builder.Build();
    }
}
