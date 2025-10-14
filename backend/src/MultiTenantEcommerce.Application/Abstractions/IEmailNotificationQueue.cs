using System.Collections.Generic;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IEmailNotificationQueue
{
    ValueTask QueueAsync(OrderEmailNotification notification, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OrderEmailNotification> DequeueAsync(CancellationToken cancellationToken);
}
