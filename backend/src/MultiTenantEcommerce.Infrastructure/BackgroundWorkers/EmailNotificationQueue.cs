using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public class EmailNotificationQueue : IEmailNotificationQueue
{
    private readonly Channel<OrderEmailNotification> _queue;

    public EmailNotificationQueue()
    {
        _queue = Channel.CreateUnbounded<OrderEmailNotification>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(OrderEmailNotification notification, CancellationToken cancellationToken = default)
        => _queue.Writer.WriteAsync(notification, cancellationToken);

    public async IAsyncEnumerable<OrderEmailNotification> DequeueAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _queue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_queue.Reader.TryRead(out var notification))
            {
                yield return notification;
            }
        }
    }
}
