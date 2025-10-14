using MultiTenantEcommerce.Maui.Views;

namespace MultiTenantEcommerce.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ProductListPage), typeof(ProductListPage));
        Routing.RegisterRoute(nameof(ProductDetailPage), typeof(ProductDetailPage));
        Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
        Routing.RegisterRoute(nameof(OrderDetailPage), typeof(OrderDetailPage));
        Routing.RegisterRoute(nameof(RefundRequestPage), typeof(RefundRequestPage));
        Routing.RegisterRoute(nameof(AddressBookPage), typeof(AddressBookPage));
        Routing.RegisterRoute(nameof(SupportTicketsPage), typeof(SupportTicketsPage));
    }
}
