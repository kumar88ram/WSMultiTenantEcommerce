using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MultiTenantEcommerce.Maui.Services;
using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui;

public partial class App : Application
{
    private readonly NotificationService _notificationService;
    private readonly ThemeService _themeService;

    public App(SplashPage splashPage, NotificationService notificationService, ThemeService themeService)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _themeService = themeService;
        MainPage = new NavigationPage(splashPage);

        _ = Task.Run(async () =>
        {
            try
            {
                await _themeService.RefreshAsync();
            }
            catch
            {
                // Theme loading is non-critical during startup; failures will be retried by view models.
            }
        });
    }

    protected override void OnStart()
    {
        base.OnStart();
        _ = _notificationService.RegisterForPushNotificationsAsync();
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);

        if (!uri.Host.Equals("payment", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var query = ParseQueryString(uri.Query);
        var status = query["status"] ?? "unknown";
        var orderId = query["orderId"] ?? string.Empty;
        NotificationService.PublishPaymentResult(status, orderId);
    }

    private static NameValueCollection ParseQueryString(string query)
    {
        var result = new NameValueCollection();
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }
}
