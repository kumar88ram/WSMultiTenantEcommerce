using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public class LoggingEmailNotificationSender : IEmailNotificationSender
{
    private readonly ILogger<LoggingEmailNotificationSender> _logger;

    public LoggingEmailNotificationSender(ILogger<LoggingEmailNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(OrderEmailNotification notification, CancellationToken cancellationToken = default)
    {
        var subject = notification.Type switch
        {
            OrderEmailNotificationType.OrderPlaced => $"Order {notification.OrderNumber} confirmed",
            OrderEmailNotificationType.OrderShipped => $"Your order {notification.OrderNumber} has shipped",
            OrderEmailNotificationType.RefundRequested => $"Refund request received for order {notification.OrderNumber}",
            OrderEmailNotificationType.RefundApproved => $"Refund approved for order {notification.OrderNumber}",
            OrderEmailNotificationType.RefundDenied => $"Refund denied for order {notification.OrderNumber}",
            OrderEmailNotificationType.RefundProcessed => $"Refund processed for order {notification.OrderNumber}",
            OrderEmailNotificationType.RefundFailed => $"Refund failed for order {notification.OrderNumber}",
            _ => $"Update for order {notification.OrderNumber}"
        };

        var amount = notification.Amount ?? notification.GrandTotal;

        _logger.LogInformation(
            "Sending email to {Email}: {Subject} (Amount: {Amount} {Currency}) {Message}",
            notification.Email,
            subject,
            amount,
            notification.Currency,
            notification.Message ?? string.Empty);

        return Task.CompletedTask;
    }
}
