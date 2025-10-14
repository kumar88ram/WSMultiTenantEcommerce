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
            _ => $"Update for order {notification.OrderNumber}"
        };

        _logger.LogInformation(
            "Sending email to {Email}: {Subject} (Total: {Amount} {Currency})",
            notification.Email,
            subject,
            notification.GrandTotal,
            notification.Currency);

        return Task.CompletedTask;
    }
}
