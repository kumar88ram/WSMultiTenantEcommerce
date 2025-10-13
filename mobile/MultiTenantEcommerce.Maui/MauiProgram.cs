using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
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

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<TokenStorageService>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();

        return builder.Build();
    }
}
