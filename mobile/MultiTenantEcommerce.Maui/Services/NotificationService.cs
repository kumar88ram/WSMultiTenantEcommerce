using CommunityToolkit.Mvvm.Messaging;
using MultiTenantEcommerce.Maui.Models;

namespace MultiTenantEcommerce.Maui.Services;

public class NotificationService
{
    public Task RegisterForPushNotificationsAsync()
    {
        // Integrate with Firebase/APNS/OneSignal in production
        return Task.CompletedTask;
    }

    public Task HandleIncomingNotificationAsync(NotificationMessage notification)
    {
        // Wire into platform specific notification handlers as needed
        return Task.CompletedTask;
    }

    public static void PublishPaymentResult(string status, string orderId)
    {
        WeakReferenceMessenger.Default.Send(new PaymentResult(status, orderId));
    }
}
