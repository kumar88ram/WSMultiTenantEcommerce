using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IEmailNotificationSender
{
    Task SendAsync(OrderEmailNotification notification, CancellationToken cancellationToken = default);
}
